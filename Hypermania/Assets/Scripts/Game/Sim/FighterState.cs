using System;
using System.Buffers.Binary;
using Design;
using MemoryPack;
using UnityEngine;
using Utils;

namespace Game.Sim
{
    public enum FighterMode
    {
        Neutral,
        Attacking,
        Hitstun,
        Blockstun,
        Knockdown,
    }

    public enum FighterLocation
    {
        Grounded,
        Airborne,
        Crouched,
    }

    public enum FighterAttackType
    {
        Invalid,
        Light,
        Medium,
        Special,
        Super,
    }

    [MemoryPackable]
    public partial struct FighterState
    {
        public Vector2 Position;
        public Vector2 Velocity;

        public FighterAttackType AttackType;
        public FighterMode Mode { get; private set; }
        public Frame ModeSt { get; private set; }

        /// <summary>
        /// The number of ticks remaining for the current mode. If the mode is Neutral or another mode that should last indefinitely, you can set
        /// this value to int.MaxValue.
        /// <br/><br/>
        /// Note that if you perform a transition in the middle of a frame, the value you set to ModeT will depend on which part of the frame you
        /// set it on. In general, if the state transition happens before physics/projectile/hurtbox calculations, ModeT should be set to the true value:
        /// i.e. a move lasting one frame (which is applied right after inputs) should set ModeT to 1. If the state transition happens after
        /// physics/projectile/hurtbox calculations, you should set ModeT to the true value + 1: i.e. a 1 frame HitStun applied after physics
        /// calculations should set ModeT to 2.
        /// </summary>
        public int ModeT;

        public Vector2 FacingDirection;

        [MemoryPackIgnore]
        public FighterLocation Location
        {
            get
            {
                if (Position.y > Globals.GROUND)
                {
                    return FighterLocation.Airborne;
                }
                return FighterLocation.Grounded;
            }
        }
        public FighterLocation LastLocation;
        public Frame LocationSt { get; private set; }

        public static FighterState Create(Vector2 position, Vector2 facingDirection, CharacterConfig characterConfig)
        {
            FighterState state = new FighterState();
            state.Position = position;
            state.Velocity = Vector2.zero;
            state.Mode = FighterMode.Neutral;
            state.ModeSt = Frame.FirstFrame;
            state.ModeT = int.MaxValue;
            state.AttackType = FighterAttackType.Invalid;
            state.FacingDirection = facingDirection;
            return state;
        }

        public void ApplyMovementIntent(Frame frame, GameInput input, CharacterConfig characterConfig)
        {
            // Horizontal movement
            switch (Mode)
            {
                case FighterMode.Neutral:
                    {
                        Velocity.x = 0;
                        if (input.Flags.HasFlag(InputFlags.Left))
                            Velocity.x = -characterConfig.Speed;
                        if (input.Flags.HasFlag(InputFlags.Right))
                            Velocity.x = characterConfig.Speed;
                        if (input.Flags.HasFlag(InputFlags.Up) && Location == FighterLocation.Grounded)
                            Velocity.y = characterConfig.JumpVelocity;
                        if (input.Flags.HasFlag(InputFlags.LightAttack))
                        {
                            switch (Location)
                            {
                                case FighterLocation.Grounded:
                                    {
                                        Velocity = Vector2.zero;
                                        Mode = FighterMode.Attacking;
                                        AttackType = FighterAttackType.Light;
                                        ModeSt = frame;
                                        ModeT = characterConfig.LightAttack.TotalTicks;
                                    }
                                    break;
                            }
                        }
                    }
                    break;
                case FighterMode.Knockdown:
                    {
                        //getup attack/rolls
                    }
                    break;
            }
        }

        public void TickStateMachine(Frame frame)
        {
            ModeT--;
            if (ModeT <= 0)
            {
                Mode = FighterMode.Neutral;
                AttackType = FighterAttackType.Invalid;
                ModeSt = frame;
                ModeT = int.MaxValue;
            }
            if (LastLocation != Location)
            {
                LastLocation = Location;
                LocationSt = frame;
            }
        }

        public void UpdatePosition(Frame frame)
        {
            // Apply gravity if not grounded
            if (Position.y > Globals.GROUND || Velocity.y > 0)
            {
                Velocity.y += Globals.GRAVITY * 1 / 60;
            }

            // Update Position
            Position += Velocity * 1 / 60;

            // Floor collision
            if (Position.y <= Globals.GROUND)
            {
                Position.y = Globals.GROUND;

                if (Velocity.y < 0)
                    Velocity.y = 0;
            }
        }
    }
}
