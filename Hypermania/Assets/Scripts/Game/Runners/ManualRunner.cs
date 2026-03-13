using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runners
{
    public class ManualRunner : LocalRunner
    {
        [SerializeField]
        private Key _advanceKey;

        [SerializeField]
        private float _holdS;

        private float _curHoldS;

        public override void Poll(float deltaTime)
        {
            if (!_initialized)
            {
                return;
            }

            for (int i = 0; i < _inputBuffers.Length; i++)
            {
                _inputBuffers[i].Saturate();
            }

            if (Keyboard.current[Key.RightArrow].wasPressedThisFrame)
            {
                GameLoop(deltaTime);
                for (int i = 0; i < _inputBuffers.Length; i++)
                {
                    _inputBuffers[i].Clear();
                }
            }
            if (Keyboard.current[Key.RightArrow].isPressed)
            {
                _curHoldS += deltaTime;
                if (_curHoldS >= _holdS)
                {
                    GameLoop(deltaTime);
                    for (int i = 0; i < _inputBuffers.Length; i++)
                    {
                        _inputBuffers[i].Clear();
                    }
                }
            }
            else
            {
                _curHoldS = 0;
            }
        }
    }
}
