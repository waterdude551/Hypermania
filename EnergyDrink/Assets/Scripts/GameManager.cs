using UnityEditor.Search;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private GameObject bob1;
    [SerializeField]
    private GameObject bob2;
    GameState currState;

    void Start()
    {
        currState = new GameState(bob1, bob2);
    }

    void Update()
    {
        Input[] inputs = new Input[2];

        Input f1Input = Input.None;
        if (UnityEngine.Input.GetKey(KeyCode.A))
            f1Input |= Input.Left;
        if (UnityEngine.Input.GetKey(KeyCode.D))
            f1Input |= Input.Right;
        if (UnityEngine.Input.GetKey(KeyCode.W))
            f1Input |= Input.Up;
        inputs[0] = f1Input;

        Input f2Input = Input.None;
        if (UnityEngine.Input.GetKey(KeyCode.LeftArrow))
            f2Input |= Input.Left;
        if (UnityEngine.Input.GetKey(KeyCode.RightArrow))
            f2Input |= Input.Right;
        if (UnityEngine.Input.GetKey(KeyCode.UpArrow))
            f2Input |= Input.Up;
        inputs[1] = f2Input;

        currState.Simulate(inputs, Time.deltaTime);
    }
}
