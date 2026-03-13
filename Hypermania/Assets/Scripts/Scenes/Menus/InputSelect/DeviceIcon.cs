using System;
using TMPro;
using UnityEngine;

namespace Scenes.Menus.InputSelect
{
    public class DeviceIcon : MonoBehaviour
    {
        [SerializeField]
        TMP_Text _deviceName;

        public void SetDeviceName(string deviceName)
        {
            _deviceName.text = deviceName;
        }
    }
}
