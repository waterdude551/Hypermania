using Design;
using Game.Sim;
using UnityEngine;
using Utils;

namespace Game.View
{
    public class GameView : MonoBehaviour
    {
        public FighterView[] Fighters => _fighters;
        private FighterView[] _fighters;
        private CharacterConfig[] _characters;

        public void Init(CharacterConfig[] characters)
        {
            _fighters = new FighterView[characters.Length];
            _characters = characters;
            for (int i = 0; i < characters.Length; i++)
            {
                _fighters[i].Init(characters[i]);
            }
        }

        public void Render(in GameState state)
        {
            for (int i = 0; i < _fighters.Length; i++)
            {
                _fighters[i].Render(state.Frame, state.Fighters[i]);
            }
        }
    }
}
