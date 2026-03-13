using Design.Configs;
using Game.Sim;
using Scenes;
using Scenes.Menus.MainMenu;
using Scenes.Menus.Session;
using UnityEngine;

namespace Scenes.Menus.InputSelect
{
    public class InputSelectDirectory : MonoBehaviour
    {
        [SerializeField]
        private DeviceManager _deviceManager;

        [SerializeField]
        private GlobalConfig _globalConfig;

        [SerializeField]
        private CharacterConfig _nytheaConfig;

        public void StartGame()
        {
            if (!_deviceManager.ValidAssignments(out var p1, out var p2))
            {
                return;
            }

            GameOptions options = new GameOptions();
            options.Global = _globalConfig;
            options.Players = new PlayerOptions[2];
            options.LocalPlayers = new LocalPlayerOptions[2];
            for (int i = 0; i < 2; i++)
            {
                options.Players[i] = new PlayerOptions
                {
                    SkinIndex = i,
                    Character = _nytheaConfig,
                    HealOnActionable = SessionDirectory.Config == GameConfig.Training,
                };
                options.LocalPlayers[i] = new LocalPlayerOptions { InputDevice = i == 0 ? p1 : p2 };
            }

            SessionDirectory.Options = options;
            SceneLoader
                .Instance.LoadNewScene()
                .Load(SceneID.Battle, SceneDatabase.BATTLE)
                .Unload(SceneID.InputSelect)
                .Unload(SceneID.MenuBase)
                .WithOverlay()
                .Execute();
        }

        public void Back()
        {
            SceneLoader
                .Instance.LoadNewScene()
                .Load(SceneID.MainMenu, SceneDatabase.MAIN_MENU)
                .Unload(SceneID.InputSelect)
                .WithOverlay()
                .Execute();
        }
    }
}
