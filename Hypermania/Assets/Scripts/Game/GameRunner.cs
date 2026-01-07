using System.Collections.Generic;
using Design;
using Design.Animation;
using Game.Sim;
using Game.View;
using Netcode.P2P;
using Netcode.Rollback;
using Steamworks;
using UnityEngine;

namespace Game
{
    public abstract class GameRunner : MonoBehaviour
    {
        [SerializeField]
        protected GameView _view;

        [SerializeField]
        protected CharacterConfigStore _characterConfigs;

        [SerializeField]
        protected bool _drawHitboxes;

        /// <summary>
        /// The current state of the runner. Must be initialized on Init();
        /// </summary>
        protected GameState _curState;

        /// <summary>
        /// The characters of each player. _characters[i] should represent the chararcter being played by handle i. Must be initialized on Init();
        /// </summary>
        protected CharacterConfig[] _characters;

        public abstract void Init(
            List<(PlayerHandle playerHandle, PlayerKind playerKind, SteamNetworkingIdentity address)> players,
            P2PClient client
        );
        public abstract void Poll(float deltaTime);
        public abstract void Stop();

        public void OnDrawGizmos()
        {
            if (_drawHitboxes)
            {
                for (int i = 0; i < _curState.Fighters.Length; i++)
                {
                    CharacterAnimation anim = _view
                        .Fighters[i]
                        .GetAnimationFromState(_curState.Frame, _curState.Fighters[i], out float _, out int ticks);
                    HitboxData data = null;
                }
            }
        }
    }
}
