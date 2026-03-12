using System;
using Design.Configs;
using Game.Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Utils.EnumArray;

namespace Game
{
    public class InputBuffer
    {
        private EnumArray<InputFlags, Binding> _controlScheme;
        private InputDevice _inputDevice;

        /**
         * Base InputBuffer Constructor
         *
         * Constructs an InputBuffer to accept user input
         *
         * @param config - The Scriptable ControlsConfig Object to Reference
         * @param inputDevice - The InputDevice to read inputs from
         *
         */
        public InputBuffer(InputDevice inputDevice, EnumArray<InputFlags, Binding> controlScheme)
        {
            _controlScheme = controlScheme;
            _inputDevice = inputDevice;
        }

        private InputFlags _input = InputFlags.None;
        private static (InputFlags dir, InputFlags opp)[] _dirPairs =
        {
            (InputFlags.Left, InputFlags.Right),
            (InputFlags.Up, InputFlags.Down),
        };

        public void Saturate()
        {
            if (_inputDevice == null)
                return;

            foreach (InputFlags flag in Enum.GetValues(typeof(InputFlags)))
            {
                if (flag == InputFlags.None)
                {
                    continue; // Skips the None InputFlag (Does Not Have a Key Press)
                }
                if (_inputDevice is Keyboard keyboard)
                {
                    if (
                        (
                            _controlScheme[flag].GetPrimaryKey() != Key.None
                            && keyboard[_controlScheme[flag].GetPrimaryKey()].isPressed
                        )
                        || (
                            _controlScheme[flag].GetAltKey() != Key.None
                            && keyboard[_controlScheme[flag].GetAltKey()].isPressed
                        )
                    )
                    {
                        _input |= flag;
                    }
                }
                else if (_inputDevice is Gamepad gamePad)
                {
                    // Checks if either the primary or alt button set in config is pressed
                    // Ignores keys set to none
                    if (
                        (
                            _controlScheme[flag].GetPrimaryGamepadButton() != GamepadButtons.None
                            && gamePad[(GamepadButton)_controlScheme[flag].GetPrimaryGamepadButton()].isPressed
                        )
                        || (
                            _controlScheme[flag].GetAltGamepadButton() != GamepadButtons.None
                            && gamePad[(GamepadButton)_controlScheme[flag].GetAltGamepadButton()].isPressed
                        )
                    )
                    {
                        _input |= flag;
                    }
                }
            }

            // clean inputs: cancel directionals
            foreach ((InputFlags dir, InputFlags opp) in _dirPairs)
            {
                if ((_input & dir) != 0 && (_input & opp) != 0)
                {
                    _input &= ~dir;
                    _input &= ~opp;
                }
            }
        }

        public void Clear()
        {
            _input = InputFlags.None;
        }

        public GameInput Poll()
        {
            return new GameInput(_input);
        }
    }
}
