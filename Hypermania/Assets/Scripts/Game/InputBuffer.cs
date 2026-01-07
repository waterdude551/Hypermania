using Game.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    // TODO: implement with actual buffer, allow customization
    public class InputBuffer
    {
        private InputFlags _curInput;

        public void Saturate()
        {
            InputFlags input = InputFlags.None;

            if (Keyboard.current.aKey.isPressed)
                input |= InputFlags.Left;
            if (Keyboard.current.dKey.isPressed)
                input |= InputFlags.Right;
            if (Keyboard.current.wKey.isPressed)
                input |= InputFlags.Up;
            if (Keyboard.current.jKey.isPressed)
                input |= InputFlags.LightAttack;
            if (Keyboard.current.kKey.isPressed)
                input |= InputFlags.MediumAttack;

            _curInput |= input;
        }

        public GameInput Consume()
        {
            GameInput res = new GameInput(_curInput);
            Debug.Log($"Consuming game input {res.Flags}");
            _curInput = InputFlags.None;
            return res;
        }
    }
}
