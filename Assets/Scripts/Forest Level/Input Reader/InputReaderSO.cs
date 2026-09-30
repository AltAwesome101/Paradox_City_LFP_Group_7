using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReaderSO", menuName = "InputReader")]
public class InputReaderSO : ScriptableObject, DefaultInputSystem.IPlayerActions
{
    DefaultInputSystem input;
    public Vector2 MoveDirection { get; private set; }
    public bool IsSprintHeld { get; private set; }

    public void EnableInputMap()
    {
        input ??= new DefaultInputSystem();
        input.Player.SetCallbacks(this);
        input.Enable();
    }

    public void DisableInputMap()
    {
        if (input == null) return;
        input.Player.SetCallbacks(null);
        input.Disable();
        input.Dispose();
        input = null;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveDirection = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        IsSprintHeld = context.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext context) { }
    public void OnLook(InputAction.CallbackContext context) { }
}