using Scenes;
using Scenes.Menus.Session;
using UnityEngine;

namespace Scenes.Menus.MainMenu
{
    public enum GameConfig
    {
        Local,
        Training,
    }

    public class MainMenuDirectory : MonoBehaviour
    {
        public void StartLocal()
        {
            SessionDirectory.Config = GameConfig.Local;
            SceneLoader
                .Instance.LoadNewScene()
                .Load(SceneID.InputSelect, SceneDatabase.INPUT_SELECT)
                .Unload(SceneID.MainMenu)
                .WithOverlay()
                .Execute();
        }

        public void StartTraining()
        {
            SessionDirectory.Config = GameConfig.Training;
            SceneLoader
                .Instance.LoadNewScene()
                .Load(SceneID.InputSelect, SceneDatabase.INPUT_SELECT)
                .Unload(SceneID.MainMenu)
                .WithOverlay()
                .Execute();
        }

        public void Quit() { }
    }
}
