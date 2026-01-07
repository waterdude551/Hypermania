using System.Collections.Generic;
using Design;
using Game.Sim;
using UnityEngine;
using Utils;

namespace Game.View
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class FighterView : MonoBehaviour
    {
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private CharacterConfig _characterConfig;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.speed = 0f;

            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(CharacterConfig characterConfig)
        {
            _characterConfig = characterConfig;
            _animator.runtimeAnimatorController = characterConfig.AnimationController;
        }

        public void Render(Frame frame, in FighterState state)
        {
            Vector3 pos = transform.position;
            pos.x = state.Position.x;
            pos.y = state.Position.y;
            transform.position = pos;

            _spriteRenderer.flipX = state.FacingDirection.x < 0f;

            CharacterAnimation animation = GetAnimationFromState(frame, state, out var normalized, out int _);

            _animator.Play(animation.ToString(), 0, normalized);
            _animator.Update(0f); // force pose evaluation this frame while paused
        }

        public CharacterAnimation GetAnimationFromState(
            Frame frame,
            in FighterState state,
            out float duration,
            out int ticks
        )
        {
            if (state.Mode == FighterMode.Attacking)
            {
                if (state.AttackType == FighterAttackType.Light)
                {
                    ticks = frame - state.ModeSt;
                    duration = (float)ticks / _characterConfig.LightAttack.TotalTicks;
                    duration -= Mathf.Floor(duration);
                    return CharacterAnimation.LightAtttack;
                }
            }

            if (state.Mode == FighterMode.Neutral)
            {
                if (state.Location == FighterLocation.Airborne)
                {
                    ticks = frame - state.ModeSt;
                    duration = (float)ticks / _characterConfig.Jump.TotalTicks;
                    duration = Mathf.Min(duration, 0.99f);
                    return CharacterAnimation.Jump;
                }
                if (state.Velocity.magnitude > 0.01f)
                {
                    ticks = frame - state.ModeSt;
                    duration = (float)ticks / _characterConfig.Walk.TotalTicks;
                    duration -= Mathf.Floor(duration);
                    return CharacterAnimation.Walk;
                }
            }
            ticks = frame - state.ModeSt;
            duration = (float)ticks / _characterConfig.Idle.TotalTicks;
            duration -= Mathf.Floor(duration);
            return CharacterAnimation.Idle;
        }
    }
}
