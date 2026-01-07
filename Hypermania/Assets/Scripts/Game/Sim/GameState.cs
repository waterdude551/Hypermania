using System;
using System.Buffers;
using Design;
using MemoryPack;
using Netcode.Rollback;
using UnityEngine;
using Utils;

namespace Game.Sim
{
    [MemoryPackable]
    public partial class GameState : IState<GameState>
    {
        public Frame Frame;
        public FighterState[] Fighters;

        // public HitboxState[] Hitboxes;
        // public ProjectileState[] Projectiles;

        /// <summary>
        /// Use this static builder instead of the constructor for creating new GameStates. This is because MemoryPack, which we use to serialize
        /// the GameState, places some funky restrictions on the constructor's paratmeter list.
        /// </summary>
        /// <param name="characterConfigs">Character configs to use</param>
        /// <returns>The created GameState</returns>
        public static GameState Create(CharacterConfig[] characters)
        {
            if (characters.Length != 2)
            {
                throw new InvalidOperationException("Must be two characters in a game state");
            }
            GameState state = new GameState();
            state.Frame = Frame.FirstFrame;
            state.Fighters = new FighterState[2];
            state.Fighters[0] = FighterState.Create(new Vector2(-7, -4.5f), Vector2.right, characters[0]);
            state.Fighters[1] = FighterState.Create(new Vector2(7, -4.5f), Vector2.left, characters[1]);
            return state;
        }

        public void Advance((GameInput input, InputStatus status)[] inputs, CharacterConfig[] characters)
        {
            Frame += 1;

            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].ApplyMovementIntent(Frame, inputs[i].input, characters[i]);
            }

            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].UpdatePosition(Frame);
            }

            // UpdateBoxes();

            // AdvanceProjectiles();

            // DetectCollisions();

            // ResolveCollisions();

            // ApplyHitResult();

            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].TickStateMachine(Frame);
            }
        }

        [ThreadStatic]
        private static ArrayBufferWriter<byte> _writer;
        private static ArrayBufferWriter<byte> Writer
        {
            get
            {
                if (_writer == null)
                    _writer = new ArrayBufferWriter<byte>(256);
                return _writer;
            }
        }

        public ulong Checksum()
        {
            Writer.Clear();
            MemoryPackSerializer.Serialize(Writer, this);
            ReadOnlySpan<byte> bytes = Writer.WrittenSpan;

            // 64-bit FNV-1a over the serialized bytes
            const ulong OFFSET = 14695981039346656037UL;
            const ulong PRIME = 1099511628211UL;

            ulong hash = OFFSET;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= PRIME;
            }
            return hash;
        }
    }
}
