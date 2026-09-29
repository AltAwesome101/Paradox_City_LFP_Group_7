using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReaderSO", menuName = "InputReader")]
public class InputReaderSO : ScriptableObject, DefaultInputSystem.IPlayerActions
{
    DefaultInputSystem input;
    public Vector2 MoveDirection{get; private set;}
    public bool IsSprintHeld{get; private set;}

    public void EnableInputMap()
    {
        if (input == null)
        {
            input = new DefaultInputSystem();
            input.Player.SetCallbacks(this);
            input.Enable();
            Debug.Log("Ïnput Reader Created");
        }
    }

    public void DisableInputMap()
    {
        if (input == null) return;
        input.Player.SetCallbacks(null);
        input.Disable();
        input.Dispose();
        input = null;
    }


    public void OnMove(InputAction.CallbackContext context){
        MoveDirection = context.ReadValue<Vector2>();
        // Debug.Log($"Input Move:{MoveDirection}");
    }

    public void OnSprint(InputAction.CallbackContext context){
        IsSprintHeld = context.ReadValueAsButton();
    }
    public void OnJump(InputAction.CallbackContext context){}
    public void OnLook(InputAction.CallbackContext context){}
}
