using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Scenes.Menus.InputSelect
{
    public class DeviceDisplay : MonoBehaviour
    {
        [SerializeField]
        private DeviceManager _deviceManager;

        [SerializeField]
        private RectTransform _inputContainer;

        [SerializeField]
        private Vector2 _spacing;

        [SerializeField]
        private GameObject _controllerPrefab;

        [SerializeField]
        private GameObject _keyboardPrefab;

        private Dictionary<InputDevice, (int pos, RectTransform icon)> _icons = new();
        private HashSet<int> _occuPos = new();

        private void OnEnable()
        {
            _deviceManager.OnDevicePair += AddDevice;
            _deviceManager.OnDeviceDisconnect += RemoveDevice;
            _deviceManager.OnDeviceChangeAssignment += OnDeviceChange;
        }

        private void OnDisable()
        {
            _deviceManager.OnDevicePair -= AddDevice;
            _deviceManager.OnDeviceDisconnect -= RemoveDevice;
            _deviceManager.OnDeviceChangeAssignment -= OnDeviceChange;
        }

        private void AddDevice(InputDevice device, DeviceType type, string deviceName)
        {
            GameObject icon = Instantiate(GetInputIcon(type), _inputContainer, false);
            int pos = 0;
            while (_occuPos.Contains(pos))
            {
                pos++;
            }
            RectTransform rect = icon.GetComponent<RectTransform>();
            DeviceIcon deviceIcon = icon.GetComponent<DeviceIcon>();
            deviceIcon.SetDeviceName(deviceName);
            _icons.Add(device, (pos, rect));
            _occuPos.Add(pos);
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, pos * -_spacing.y);
        }

        private void RemoveDevice(InputDevice device)
        {
            if (_icons.Remove(device, out var data))
            {
                Destroy(data.icon);
                _occuPos.Remove(data.pos);
            }
        }

        private void OnDeviceChange(InputDevice device, DeviceAssignment assignment)
        {
            _icons[device].icon.anchoredPosition = new Vector2(
                ((int)assignment - 1) * _spacing.x,
                _icons[device].icon.anchoredPosition.y
            );
        }

        private GameObject GetInputIcon(DeviceType type)
        {
            if ((type & DeviceType.Gamepad) != 0)
                return _controllerPrefab;

            if ((type & DeviceType.Keyboard) != 0)
                return _keyboardPrefab;

            return null;
        }
    }
}
