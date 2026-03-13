using Game;
using Game.Sim;
using Scenes.Menus.Session;
using UnityEngine;

namespace Scenes.Menus.Battle
{
    public class BattleDirectory : MonoBehaviour
    {
        [SerializeField]
        private GameManager _gameManager;

        public void Start()
        {
            GameOptions options = SessionDirectory.Options;
            _gameManager.StartLocalGame(options);
        }
    }
}
