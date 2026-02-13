using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class InputReader : MonoBehaviour
{
    public float MoveX { get; private set; }

    private InputActions _input;

    private void Awake()
    {
        _input = new InputActions();
    }

    private void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.Move.performed += OnMove;
        _input.Player.Move.canceled += OnMove;
    }

    private void OnDisable()
    {
        _input.Player.Move.performed -= OnMove;
        _input.Player.Move.canceled -= OnMove;
        _input.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveX = context.ReadValue<float>();
    }
}