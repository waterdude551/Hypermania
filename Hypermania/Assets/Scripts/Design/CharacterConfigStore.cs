using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Design
{
    [CreateAssetMenu(menuName = "Hypermania/Character Config Store")]
    public class CharacterConfigStore : ScriptableObject
    {
        [SerializeField]
        private List<CharacterConfig> _configs;

        public CharacterConfig Get(Character character)
        {
            foreach (CharacterConfig config in _configs)
            {
                if (config.Character == character)
                {
                    return config;
                }
            }
            return null;
        }
    }
}
