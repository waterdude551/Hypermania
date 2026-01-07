using System;
using Design.Animation;
using Game;
using UnityEngine;

namespace Design
{
    [CreateAssetMenu(menuName = "Hypermania/Character Config")]
    public class CharacterConfig : ScriptableObject
    {
        public Character Character;
        public AnimatorOverrideController AnimationController;
        public float Speed;
        public float JumpVelocity;
        public HitboxData Walk;
        public HitboxData Idle;
        public HitboxData LightAttack;
        public HitboxData Jump;

        // TODO: many more

        public HitboxData GetHitboxData(CharacterAnimation anim)
        {
            switch (anim)
            {
                case CharacterAnimation.Walk:
                    return Walk;
                case CharacterAnimation.Idle:
                    return Idle;
                case CharacterAnimation.Jump:
                    return Jump;
                case CharacterAnimation.LightAtttack:
                    return LightAttack;
                default:
                    throw new InvalidOperationException(
                        "Tried to get hitbox data for a move not registered. Did you add a new type of animation and forget to add it to CharacterConfig.GetHitboxData()?"
                    );
            }
        }
    }
}
