using UnityEngine;


public class TimeMachinePortal : MonoBehaviour
{
    [Tooltip("Which level this specific time machine leads to. Just a label for now - " +
             "e.g. \"VennaAcademy\", \"AppleForest\" or \"WrongBeer\"")]
    public string destinationLevel;

    
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        CarController car = other.GetComponentInParent<CarController>();
        if (car == null) return; 

        hasTriggered = true;
        LoadLevel();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<CarController>() != null)
        {
            hasTriggered = false;
        }
    }

    private void LoadLevel()
    {
       
        Debug.Log($"[TimeMachinePortal] Car drove through \"{gameObject.name}\" \u2192 would load: {destinationLevel}");
    }
}