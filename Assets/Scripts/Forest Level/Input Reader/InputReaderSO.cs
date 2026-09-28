using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReaderSO", menuName = "InputReader")]
public class InputReaderSO : ScriptableObject, DefaultInputSystem.IPlayerActions
{
    public Vector2 MoveDirection{get; private set;}
    public bool IsSprintHeld{get; private set;}

    public void OnJump(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveDirection = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        IsSprintHeld = context.ReadValueAsButton();
    }
}
