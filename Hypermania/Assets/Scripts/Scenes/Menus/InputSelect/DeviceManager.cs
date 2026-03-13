using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering.VirtualTexturing;

namespace Scenes.Menus.InputSelect
{
    //Quick bitmask to check for legal device connection types. I'm pretty sure
    //it's just keyboard or controller but you never know if someone brings the
    //legendary Osu drawing tablet
    [Flags]
    public enum DeviceType
    {
        None = 0,
        Keyboard = 1 << 0,
        Mouse = 1 << 1,
        Gamepad = 1 << 2,
        Touch = 1 << 3,

        All = ~0,
    }

    public enum DeviceAssignment
    {
        Player1 = 0,
        None = 1,
        Player2 = 2,
    }

    public class DeviceManager : MonoBehaviour
    {
        private const int LISTENING = 1;
        private const int DISABLED = 0;

        [SerializeField]
        private DeviceType _legalDevices = DeviceType.Keyboard | DeviceType.Gamepad;

        [SerializeField]
        private bool _debug = false;

        public Dictionary<InputDevice, DeviceAssignment> RegisteredDevices { get; private set; } = new();

        public delegate void DevicePair(InputDevice device, DeviceType deviceType, string displayName);
        public event DevicePair OnDevicePair;

        public delegate void DeviceDisconnect(InputDevice device);
        public event DeviceDisconnect OnDeviceDisconnect;

        public delegate void DeviceChangeAssignment(InputDevice device, DeviceAssignment assignment);
        public event DeviceChangeAssignment OnDeviceChangeAssignment;

        public bool ValidAssignments(out InputDevice player1, out InputDevice player2)
        {
            player1 = RegisteredDevices.FirstOrDefault((kvp) => kvp.Value == DeviceAssignment.Player1).Key;
            player2 = RegisteredDevices.FirstOrDefault((kvp) => kvp.Value == DeviceAssignment.Player2).Key;
            int oneCount = RegisteredDevices.Count((kvp) => kvp.Value == DeviceAssignment.Player1);
            int twoCount = RegisteredDevices.Count((kvp) => kvp.Value == DeviceAssignment.Player2);
            return oneCount <= 1 && twoCount <= 1 && oneCount + twoCount > 0;
        }

        private void OnEnable()
        {
            InputUser.listenForUnpairedDeviceActivity = LISTENING;
            InputUser.onUnpairedDeviceUsed += RegisterDevice;
            InputSystem.onDeviceChange += DeregisterDevice;
        }

        private void Update()
        {
            HandlePlayerInputSelect();
        }

        private void OnDisable()
        {
            InputUser.listenForUnpairedDeviceActivity = DISABLED;
            InputUser.onUnpairedDeviceUsed -= RegisterDevice;
            InputSystem.onDeviceChange -= DeregisterDevice;
        }

        private void RegisterDevice(InputControl control, InputEventPtr ptr)
        {
            InputDevice device = control.device;
            DeviceType deviceType = DeviceType.None;

            if (RegisteredDevices.ContainsKey(device))
                return;

            //Device validity check
            if (device is Keyboard)
                deviceType = DeviceType.Keyboard;
            else if (device is Mouse)
                deviceType = DeviceType.Mouse;
            else if (device is Gamepad)
                deviceType = DeviceType.Gamepad;
            else if (device is Touchscreen)
                deviceType = DeviceType.Touch;

            if ((_legalDevices & deviceType) == 0)
                return;

            if (_debug)
                Debug.Log($"Device {device.displayName} joined.");

            RegisteredDevices.Add(device, DeviceAssignment.None);
            InputUser.PerformPairingWithDevice(device);

            OnDevicePair?.Invoke(device, deviceType, device.name);
        }

        private void DeregisterDevice(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Disconnected || change == InputDeviceChange.Removed)
            {
                if (RegisteredDevices.Remove(device))
                {
                    if (_debug)
                        Debug.Log($"Device {device.name} has disconnected.");
                    OnDeviceDisconnect?.Invoke(device);
                }
            }
        }

        private void HandlePlayerInputSelect()
        {
            Dictionary<InputDevice, (bool left, bool right)> gatheredInputs = new();
            foreach (InputDevice device in RegisteredDevices.Keys)
            {
                bool left = false;
                bool right = false;
                switch (device)
                {
                    case Gamepad gamepad:
                        left =
                            gamepad.leftTrigger.wasPressedThisFrame
                            || gamepad.leftShoulder.wasPressedThisFrame
                            || gamepad.dpad.left.wasPressedThisFrame
                            || gamepad.leftStick.x.value < -0.5f
                            || gamepad.rightStick.x.value < -0.5f;
                        right =
                            gamepad.rightTrigger.wasPressedThisFrame
                            || gamepad.rightShoulder.wasPressedThisFrame
                            || gamepad.dpad.right.wasPressedThisFrame
                            || gamepad.leftStick.x.value > 0.5f
                            || gamepad.rightStick.x.value > 0.5f;
                        break;
                    case Keyboard keyboard:
                        left = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
                        right = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;
                        break;
                }
                gatheredInputs[device] = (left, right);
            }

            foreach ((InputDevice device, (bool left, bool right)) in gatheredInputs)
            {
                if (left)
                {
                    DeviceAssignment n = (DeviceAssignment)Math.Clamp((int)RegisteredDevices[device] - 1, 0, 2);
                    if (n != RegisteredDevices[device])
                    {
                        OnDeviceChangeAssignment?.Invoke(device, n);
                    }
                    RegisteredDevices[device] = n;
                }
                if (right)
                {
                    DeviceAssignment n = (DeviceAssignment)Math.Clamp((int)RegisteredDevices[device] + 1, 0, 2);
                    if (n != RegisteredDevices[device])
                    {
                        OnDeviceChangeAssignment?.Invoke(device, n);
                    }
                    RegisteredDevices[device] = n;
                }
            }
        }
    }
}
