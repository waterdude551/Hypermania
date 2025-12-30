using UnityEngine;

public struct GameState
{
    public FighterInfo F1Info;
    public FighterInfo F2Info;

    public GameState(GameObject bob1, GameObject bob2)
    {
        F1Info = new FighterInfo(bob1, new Vector2(-9, -4.5f), Vector2.zero, 7f, FighterState.Idle, 0, Vector2.right);
        F2Info = new FighterInfo(bob2, new Vector2(9, -4.5f), Vector2.zero, 7f, FighterState.Idle, 0, Vector2.left);
    }

    public void Simulate(Input[] inputs, float deltaTime)
    {
        F1Info.HandleInput(inputs[0], deltaTime);
        F2Info.HandleInput(inputs[1], deltaTime);
        Debug.Log("Bob 1 State: " + F1Info.State);
    }
}
