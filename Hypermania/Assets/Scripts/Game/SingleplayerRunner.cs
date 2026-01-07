using System;
using System.Collections.Generic;
using Design;
using Game.Sim;
using Netcode.P2P;
using Netcode.Rollback;
using Netcode.Rollback.Sessions;
using Steamworks;
using UnityEngine;

namespace Game
{
    public class SingleplayerRunner : GameRunner
    {
        protected SyncTestSession<GameState, GameInput, SteamNetworkingIdentity> _session;
        protected bool _initialized;
        protected float _time;
        private InputBuffer _inputBuffer;

        protected void OnEnable()
        {
            _curState = null;
            _session = null;
            _initialized = false;
            _time = 0;
            _characters = null;
            _inputBuffer = null;
        }

        protected void OnDisable()
        {
            _curState = null;
            _session = null;
            _initialized = false;
            _time = 0;
            _characters = null;
            _inputBuffer = null;
        }

        public override void Init(
            List<(PlayerHandle playerHandle, PlayerKind playerKind, SteamNetworkingIdentity address)> players,
            P2PClient client
        )
        {
            // TODO: take in character selections from matchmaking/lobby
            CharacterConfig sampleConfig = _characterConfigs.Get(Character.SampleFighter);
            _characters = new CharacterConfig[2] { sampleConfig, sampleConfig };

            _curState = GameState.Create(_characters);
            SessionBuilder<GameInput, SteamNetworkingIdentity> builder = new SessionBuilder<
                GameInput,
                SteamNetworkingIdentity
            >()
                .WithNumPlayers(players.Count)
                .WithFps(64);
            foreach ((PlayerHandle playerHandle, PlayerKind playerKind, SteamNetworkingIdentity address) in players)
            {
                if (playerKind != PlayerKind.Local)
                {
                    throw new InvalidOperationException("Cannot have remote/spectators in a local session");
                }
                builder.AddPlayer(
                    new PlayerType<SteamNetworkingIdentity> { Kind = playerKind, Address = address },
                    playerHandle
                );
            }
            _session = builder.StartSynctestSession<GameState>();
            _inputBuffer = new InputBuffer();
            _view.Init(_characters);
            _initialized = true;
        }

        public override void Stop()
        {
            OnDisable();
        }

        public override void Poll(float deltaTime)
        {
            if (!_initialized)
            {
                return;
            }

            _inputBuffer.Saturate();

            float fpsDelta = 1.0f / GameManager.TPS;
            _time += deltaTime;

            while (_time > fpsDelta)
            {
                _time -= fpsDelta;
                GameLoop();
            }
        }

        protected void GameLoop()
        {
            if (_session == null)
            {
                return;
            }

            _session.AddLocalInput(new PlayerHandle(0), _inputBuffer.Consume());
            try
            {
                List<RollbackRequest<GameState, GameInput>> requests = _session.AdvanceFrame();
                foreach (RollbackRequest<GameState, GameInput> request in requests)
                {
                    switch (request.Kind)
                    {
                        case RollbackRequestKind.SaveGameStateReq:
                            var saveReq = request.GetSaveGameStateReq();
                            saveReq.Cell.Save(saveReq.Frame, _curState, _curState.Checksum());
                            break;
                        case RollbackRequestKind.LoadGameStateReq:
                            var loadReq = request.GetLoadGameStateReq();
                            loadReq.Cell.Load(out _curState);
                            break;
                        case RollbackRequestKind.AdvanceFrameReq:
                            _curState.Advance(request.GetAdvanceFrameRequest().Inputs, _characters);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[Game] Exception {e}");
            }

            _view.Render(_curState);
        }
    }
}
