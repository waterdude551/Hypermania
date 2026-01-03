using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Netcode.Rollback;
using Netcode.Rollback.Network;
using Steamworks;

public sealed class SteamMatchmakingClient : IDisposable, INonBlockingSocket<CSteamID>
{
    // ---------- Public API ----------
    public CSteamID CurrentLobby => _currentLobby;
    public bool InLobby => _currentLobby.IsValid();
    public bool HasPeer => _peer.IsValid();
    public CSteamID Me => SteamUser.GetSteamID();
    public CSteamID Peer => _peer;

    /// <summary>
    /// After StartGame completes, provides stable contiguous handles in [0, maxPlayers)
    /// for each lobby member. For 1v1, this is {host=0, other=1}.
    /// </summary>
    public IReadOnlyDictionary<CSteamID, int> Handles => _handles;
    public int MyHandle => Handles[Me];

    public event Action<CSteamID> OnStartGame;

    public SteamMatchmakingClient()
    {
        RegisterCallbacks();
    }

    public async Task<CSteamID> Create(int maxMembers = 2)
    {
        if (maxMembers <= 0) throw new ArgumentOutOfRangeException(nameof(maxMembers));
        EnsureNotDisposed();

        await Leave().ConfigureAwait(false);

        _maxMembers = maxMembers;

        _lobbyCreatedTcs = new TaskCompletionSource<CSteamID>(TaskCreationOptions.RunContinuationsAsynchronously);
        var call = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, maxMembers);
        _lobbyCreatedCallResult.Set(call);

        var lobbyId = await _lobbyCreatedTcs.Task.ConfigureAwait(false);
        _currentLobby = lobbyId;

        SteamMatchmaking.SetLobbyData(_currentLobby, "version", "1");
        SteamMatchmaking.SetLobbyData(_currentLobby, "game", "EnergyDrink");

        // Store max members so joiners can compute consistent handle ordering.
        SteamMatchmaking.SetLobbyData(_currentLobby, "maxMembers", maxMembers.ToString());

        RefreshPeerFromLobby();
        return lobbyId;
    }

    public async Task Join(CSteamID lobbyId)
    {
        if (!lobbyId.IsValid()) throw new ArgumentException("Invalid lobby id.", nameof(lobbyId));
        EnsureNotDisposed();

        await Leave().ConfigureAwait(false);

        _lobbyEnterTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        SteamMatchmaking.JoinLobby(lobbyId);

        await _lobbyEnterTcs.Task.ConfigureAwait(false);
        _currentLobby = lobbyId;

        // Read max members written by host if present.
        if (int.TryParse(SteamMatchmaking.GetLobbyData(_currentLobby, "maxMembers"), out int mm) && mm > 0)
            _maxMembers = mm;

        RefreshPeerFromLobby();
    }

    public Task Leave()
    {
        EnsureNotDisposed();

        CloseConnection();

        if (_currentLobby.IsValid())
        {
            SteamMatchmaking.LeaveLobby(_currentLobby);
            _currentLobby = default;
        }

        _peer = default;
        _startArmed = false;
        _startSentByHost = false;
        _handles.Clear();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the game and returns contiguous handles for each SteamID in the lobby.
    /// Ordering rule: lobby owner gets handle 0, remaining members sorted by SteamID (ascending) get 1..N-1.
    /// This is deterministic on all clients.
    /// </summary>
    public Task<Dictionary<CSteamID, int>> StartGame()
    {
        EnsureNotDisposed();
        if (!_currentLobby.IsValid()) throw new InvalidOperationException("Not in a lobby.");

        ConfigureP2P();
        RefreshPeerFromLobby();
        if (!_peer.IsValid()) throw new InvalidOperationException("No peer in lobby yet.");

        _startArmed = true;
        _startGameTcs = new TaskCompletionSource<Dictionary<CSteamID, int>>(TaskCreationOptions.RunContinuationsAsynchronously);

        bool iAmHost = SteamMatchmaking.GetLobbyOwner(_currentLobby) == Me;

        if (iAmHost)
        {
            // Host computes and publishes handle map so clients can validate (optional).
            ComputeHandlesDeterministic();
            PublishHandlesToLobby();

            SendLobbyStartMessage();
            _startSentByHost = true;

            EnsureConnectionTo(_peer);
        }
        else
        {
            // Wait for host's "__START__" in OnLobbyChatMessage, then connect.
            // Handles will be computed deterministically upon connect (and can be verified against lobby data).
        }

        return _startGameTcs.Task;
    }

    // ---------- INonBlockingSocket<CSteamID> ----------
    public void SendTo(in Message message, CSteamID addr)
    {
        EnsureNotDisposed();
        if (!addr.IsValid()) throw new ArgumentException("Invalid addr.", nameof(addr));

        var conn = EnsureConnectionTo(addr);

        byte[] payload = new byte[message.SerdeSize()];
        message.Serialize(payload);

        unsafe
        {
            fixed (byte* pData = payload)
            {
                var res = SteamNetworkingSockets.SendMessageToConnection(
                    conn,
                    (IntPtr)pData,
                    (uint)payload.Length,
                    Constants.k_nSteamNetworkingSend_UnreliableNoNagle,
                    out _);

                if (res != EResult.k_EResultOK)
                    throw new InvalidOperationException($"SendMessageToConnection failed: {res}");
            }
        }
    }

    public List<(CSteamID addr, Message message)> ReceiveAllMessages()
    {
        EnsureNotDisposed();
        var received = new List<(CSteamID addr, Message message)>();

        if (_conn == HSteamNetConnection.Invalid)
            return received;

        const int BATCH = 32;
        IntPtr[] ptrs = new IntPtr[BATCH];

        while (true)
        {
            int n = SteamNetworkingSockets.ReceiveMessagesOnConnection(_conn, ptrs, BATCH);
            if (n <= 0) break;

            for (int i = 0; i < n; i++)
            {
                var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(ptrs[i]);

                try
                {
                    byte[] data = new byte[msg.m_cbSize];
                    Marshal.Copy(msg.m_pData, data, 0, msg.m_cbSize);

                    Message decoded = default;
                    decoded.Deserialize(data);

                    // 1v1: only one peer connection
                    received.Add((_peer, decoded));
                }
                finally
                {
                    SteamNetworkingMessage_t.Release(ptrs[i]);
                }
            }
        }

        return received;
    }

    // ---------- Internals ----------
    private const string START_MSG = "__START__";
    private const string HANDLES_KEY = "handles"; // "steamid:handle,steamid:handle,..."

    private bool _disposed;

    private int _maxMembers = 2;

    private CSteamID _currentLobby;
    private CSteamID _peer;

    private readonly Dictionary<CSteamID, int> _handles = new();

    private HSteamNetConnection _conn = HSteamNetConnection.Invalid;
    private HSteamListenSocket _listen = HSteamListenSocket.Invalid;

    private Callback<LobbyEnter_t> _lobbyEnterCb;
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdateCb;
    private Callback<LobbyChatMsg_t> _lobbyChatMsgCb;
    private Callback<GameLobbyJoinRequested_t> _joinRequestedCb;

    private Callback<SteamNetConnectionStatusChangedCallback_t> _connStatusCb;

    private CallResult<LobbyCreated_t> _lobbyCreatedCallResult;
    private TaskCompletionSource<CSteamID> _lobbyCreatedTcs;
    private TaskCompletionSource<bool> _lobbyEnterTcs;

    private TaskCompletionSource<Dictionary<CSteamID, int>> _startGameTcs;
    private bool _startArmed;
    private bool _startSentByHost;

    private void RegisterCallbacks()
    {
        _lobbyEnterCb = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        _lobbyChatUpdateCb = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        _lobbyChatMsgCb = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);
        _joinRequestedCb = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);

        _connStatusCb = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnNetConnectionStatusChanged);

        _lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
    }

    // should not be called in awake (could happen before SteamAPI.Init() is called)
    private void ConfigureP2P()
    {
        try
        {
            SteamNetworkingUtils.SetConfigValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable,
                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global,
                IntPtr.Zero,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                new IntPtr(1));

            SteamNetworkingUtils.SetConfigValue(
                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_SDR_Penalty,
                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global,
                IntPtr.Zero,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                new IntPtr(0));
        }
        catch
        {
            // Binding differences across Steamworks.NET versions; ignore if unavailable.
        }
    }

    private void OnLobbyCreated(LobbyCreated_t data, bool ioFailure)
    {
        if (_lobbyCreatedTcs == null) return;

        if (ioFailure || data.m_eResult != EResult.k_EResultOK)
        {
            _lobbyCreatedTcs.TrySetException(
                new InvalidOperationException($"CreateLobby failed: ioFailure={ioFailure}, result={data.m_eResult}"));
            return;
        }

        _lobbyCreatedTcs.TrySetResult(new CSteamID(data.m_ulSteamIDLobby));
    }

    private void OnLobbyEnter(LobbyEnter_t data)
    {
        bool ok = data.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess;
        if (!ok)
        {
            _lobbyEnterTcs?.TrySetException(
                new InvalidOperationException($"JoinLobby failed: EChatRoomEnterResponse={data.m_EChatRoomEnterResponse}"));
            return;
        }

        _lobbyEnterTcs?.TrySetResult(true);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t data)
    {
        if (!_currentLobby.IsValid() || data.m_ulSteamIDLobby != _currentLobby.m_SteamID)
            return;

        RefreshPeerFromLobby();
    }

    private void OnLobbyChatMessage(LobbyChatMsg_t data)
    {
        if (!_currentLobby.IsValid() || data.m_ulSteamIDLobby != _currentLobby.m_SteamID)
            return;

        CSteamID user;
        EChatEntryType type;
        byte[] buffer = new byte[256];

        int len = SteamMatchmaking.GetLobbyChatEntry(
            new CSteamID(data.m_ulSteamIDLobby),
            (int)data.m_iChatID,
            out user,
            buffer,
            buffer.Length,
            out type);

        if (len <= 0) return;

        string text = System.Text.Encoding.UTF8.GetString(buffer, 0, len).TrimEnd('\0');

        if (text == START_MSG)
        {
            _startSentByHost = true;

            // Compute handles deterministically on all clients (host+joiners).
            ComputeHandlesDeterministic();

            // Optionally verify against host-published lobby data (if present).
            TryVerifyHandlesFromLobbyData();

            RefreshPeerFromLobby();
            if (_peer.IsValid())
                EnsureConnectionTo(_peer);
        }
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t data)
    {
        // Optional: auto-join when invited / clicked join.
        // SteamMatchmaking.JoinLobby(data.m_steamIDLobby);
    }

    private void RefreshPeerFromLobby()
    {
        _peer = default;
        if (!_currentLobby.IsValid()) return;

        int count = SteamMatchmaking.GetNumLobbyMembers(_currentLobby);
        for (int i = 0; i < count; i++)
        {
            var member = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobby, i);
            if (member.IsValid() && member != Me)
            {
                _peer = member;
                break;
            }
        }
    }

    private void SendLobbyStartMessage()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(START_MSG);
        SteamMatchmaking.SendLobbyChatMsg(_currentLobby, bytes, bytes.Length);
    }

    private void ComputeHandlesDeterministic()
    {
        _handles.Clear();
        if (!_currentLobby.IsValid()) return;

        int count = SteamMatchmaking.GetNumLobbyMembers(_currentLobby);
        if (count <= 0) return;

        // Cap to maxMembers, but do not silently remap weird states.
        int n = Math.Min(count, Math.Max(1, _maxMembers));

        var owner = SteamMatchmaking.GetLobbyOwner(_currentLobby);

        var others = new List<CSteamID>(n);
        for (int i = 0; i < count; i++)
        {
            var m = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobby, i);
            if (!m.IsValid()) continue;
            if (m == owner) continue;
            others.Add(m);
        }

        // Deterministic tie-break: SteamID ascending.
        others.Sort((a, b) => a.m_SteamID.CompareTo(b.m_SteamID));

        _handles[owner] = 0;

        int handle = 1;
        for (int i = 0; i < others.Count && handle < _maxMembers; i++, handle++)
            _handles[others[i]] = handle;
    }

    private void PublishHandlesToLobby()
    {
        // host-only; joiners can read/verify
        if (!_currentLobby.IsValid()) return;

        // "steamid:handle,steamid:handle"
        var parts = new List<string>(_handles.Count);
        foreach (var kv in _handles)
            parts.Add($"{kv.Key.m_SteamID}:{kv.Value}");

        SteamMatchmaking.SetLobbyData(_currentLobby, HANDLES_KEY, string.Join(",", parts));
    }

    private void TryVerifyHandlesFromLobbyData()
    {
        // Optional sanity check.
        // If host wrote handles, ensure we match.
        string s = SteamMatchmaking.GetLobbyData(_currentLobby, HANDLES_KEY);
        if (string.IsNullOrEmpty(s)) return;

        var parsed = new Dictionary<ulong, int>();
        var entries = s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var e in entries)
        {
            var kv = e.Split(':');
            if (kv.Length != 2) continue;
            if (!ulong.TryParse(kv[0], out ulong sid)) continue;
            if (!int.TryParse(kv[1], out int h)) continue;
            parsed[sid] = h;
        }

        foreach (var kv in _handles)
        {
            if (!parsed.TryGetValue(kv.Key.m_SteamID, out int h) || h != kv.Value)
            {
                // If mismatch, prefer deterministic local mapping; you can throw if you want strictness.
                return;
            }
        }
    }

    private HSteamNetConnection EnsureConnectionTo(CSteamID peer)
    {
        if (!peer.IsValid()) throw new InvalidOperationException("Peer invalid.");

        if (_conn != HSteamNetConnection.Invalid)
            return _conn;

        if (_listen == HSteamListenSocket.Invalid)
            _listen = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);

        var id = new SteamNetworkingIdentity();
        id.SetSteamID(peer);

        _conn = SteamNetworkingSockets.ConnectP2P(ref id, 0, 0, null);

        if (_conn == HSteamNetConnection.Invalid)
            throw new InvalidOperationException("ConnectP2P returned invalid connection handle.");

        return _conn;
    }

    private void OnNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t data)
    {
        if (_conn == HSteamNetConnection.Invalid)
            _conn = data.m_hConn;

        switch (data.m_info.m_eState)
        {
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                {
                    SteamNetworkingSockets.AcceptConnection(data.m_hConn);
                    break;
                }

            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                {
                    if (_startArmed && _startSentByHost)
                    {
                        if (!_peer.IsValid())
                        {
                            var remoteId = data.m_info.m_identityRemote;
                            if (remoteId.GetSteamID().IsValid())
                                _peer = remoteId.GetSteamID();
                        }

                        // Ensure handles exist even if host didn't publish.
                        if (_handles.Count == 0)
                        {
                            ComputeHandlesDeterministic();
                            TryVerifyHandlesFromLobbyData();
                        }

                        // Return a copy to avoid external mutation.
                        _startGameTcs?.TrySetResult(new Dictionary<CSteamID, int>(_handles));
                        OnStartGame?.Invoke(_peer);
                    }
                    break;
                }

            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                {
                    if (_conn != HSteamNetConnection.Invalid)
                    {
                        SteamNetworkingSockets.CloseConnection(_conn, 0, "closed", false);
                        _conn = HSteamNetConnection.Invalid;
                    }

                    _startGameTcs?.TrySetException(
                        new InvalidOperationException($"Connection closed: state={data.m_info.m_eState}, endReason={data.m_info.m_eEndReason}"));
                    break;
                }
        }
    }

    private void CloseConnection()
    {
        if (_conn != HSteamNetConnection.Invalid)
        {
            SteamNetworkingSockets.CloseConnection(_conn, 0, "leaving", false);
            _conn = HSteamNetConnection.Invalid;
        }

        if (_listen != HSteamListenSocket.Invalid)
        {
            SteamNetworkingSockets.CloseListenSocket(_listen);
            _listen = HSteamListenSocket.Invalid;
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SteamMatchmakingClient));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CloseConnection();

        _lobbyEnterCb?.Unregister();
        _lobbyChatUpdateCb?.Unregister();
        _lobbyChatMsgCb?.Unregister();
        _joinRequestedCb?.Unregister();
        _connStatusCb?.Unregister();
    }
}
