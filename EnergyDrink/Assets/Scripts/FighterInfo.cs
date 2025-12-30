
using System;
using Unity.VisualScripting;
using UnityEngine;

public enum FighterState
{
    Idle,
    Walking,
    Dashing,
    Jumping,
    Falling,
    Stunned,
    Attacking,
    Grabbing
}

public struct FighterInfo
{
    public GameObject Go;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Speed;
    public FighterState State;
    public int StateFrameCount;
    public Vector2 FacingDirection;

    public FighterInfo(GameObject go, Vector2 position, Vector2 velocity, float speed, FighterState state, int stateFrameCount, Vector2 facingDirection)
    {
        Go = go;
        Position = position;
        go.transform.position = Position;
        Velocity = velocity;
        Speed = speed;
        State = state;
        StateFrameCount = stateFrameCount;
        FacingDirection = facingDirection;
    }

    public void HandleInput(Input input, float deltaTime)
    {
        // Horizontal movement
        Velocity.x = 0;
        if (input.HasFlag(Input.Left))
            Velocity.x = -Speed;
        if (input.HasFlag(Input.Right))
            Velocity.x = Speed;

        // Vertical movement only if grounded
        if (input.HasFlag(Input.Up) && Position.y <= Globals.GROUND)
        {
            Velocity.y = Speed * 1.5f;
        }
        UpdatePhysics(deltaTime);
    }
    public void UpdatePhysics(float deltaTime)
    {
        // Apply gravity if not grounded
        if (Position.y > Globals.GROUND || Velocity.y > 0)
        {
            Velocity.y += Globals.GRAVITY * deltaTime;
        }

        // Update Position
        Position += Velocity * deltaTime;

        // Floor collision
        if (Position.y <= Globals.GROUND)
        {
            Position.y = Globals.GROUND;

            // Only zero vertical velocity if falling
            if (State == FighterState.Falling)
                Velocity.y = 0;
        }

        // Update GameObject
        if (Go != null)
            Go.transform.position = Position;

        UpdateState();
    }
    public void UpdateState()
    {
        if (Velocity.y > 0)
        {
            State = FighterState.Jumping;
        }
        else if (Velocity.y < 0)
        {
            State = FighterState.Falling;
        }
        else if (Velocity.x != 0)
        {
            State = FighterState.Walking;
        }
        else
        {
            State = FighterState.Idle;
        }
    }
}
