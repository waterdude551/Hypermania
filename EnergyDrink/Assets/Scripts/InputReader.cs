using System;
using UnityEngine;

// Input is an enum that uses the Flags attribute, which means that it can use bitwise operations on initialization and when checking whether certain enum values are present in an Input.
// For example, if the user presses left and up, we would set the Input = Input.Left | Input.Up, which sets the bits accordingly.
// To check if the user has pressed down, we can use userInput.HasFlag(Input.Down).
[Flags]
public enum Input
{
    None = 0,
    Up = 1 << 1,
    Down = 1 << 2,
    Left = 1 << 3,
    Right = 1 << 4,
    LightAttack = 1 << 5,
    MediumAttack = 1 << 6,
    HeavyAttack = 1 << 7,
    Grab = 1 << 8
}
public class InputReader
{
    public static Input ReadKeyboardWASD()
    {
        Input input = Input.None;

        if (UnityEngine.Input.GetKey(KeyCode.W)) input |= Input.Up;
        if (UnityEngine.Input.GetKey(KeyCode.S)) input |= Input.Down;
        if (UnityEngine.Input.GetKey(KeyCode.A)) input |= Input.Left;
        if (UnityEngine.Input.GetKey(KeyCode.D)) input |= Input.Right;

        return input;
    }
}
