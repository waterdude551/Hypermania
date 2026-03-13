using UnityEngine;

namespace Scenes
{
    public class CoreDirectory : MonoBehaviour
    {
        public void Start()
        {
            SceneLoader
                .Instance.LoadNewScene()
                .Load(SceneID.Session, SceneDatabase.SESSION)
                .Load(SceneID.MenuBase, SceneDatabase.MENU_BASE)
                .Load(SceneID.MainMenu, SceneDatabase.MAIN_MENU)
                .WithOverlay()
                .Execute();
        }
    }
}
