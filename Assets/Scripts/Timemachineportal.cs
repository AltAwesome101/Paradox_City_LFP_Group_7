using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeMachinePortal : MonoBehaviour
{
    [Tooltip("Which level this specific time machine leads to. Just a label for now - " +
             "e.g. \"VennaAcademy\", \"AppleForest\" or \"WrongBeer\"")]
    public string destinationLevel;

    [Header("Optional")]
    [Tooltip("If you're using TextMeshPro world-space text above the portal, drop it here " +
             "and it'll auto-fill with destinationLevel on start (one less place to edit).")]
    [SerializeField] TMPro.TextMeshPro destinationLabel;

    bool hasTriggered = false;

    void Awake()
    {
        if (destinationLabel != null)
            destinationLabel.text = destinationLevel;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TimeMachinePortal] OnTriggerEnter on \"{gameObject.name}\" by \"{other.name}\" " +
                  $"(tag={other.tag}, layer={LayerMask.LayerToName(other.gameObject.layer)}, hasTriggered={hasTriggered})");

        if (hasTriggered) return;
        if (!IsValidEntrant(other, out var label))
        {
            Debug.Log($"[TimeMachinePortal] \"{other.name}\" did not match car or player checks - ignoring.");
            return;
        }

        hasTriggered = true;
        LoadLevel(label);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"[TimeMachinePortal] OnTriggerExit on \"{gameObject.name}\" by \"{other.name}\"");

        if (IsValidEntrant(other, out _))
            hasTriggered = false;
    }

    // Everything allowed to activate the portal goes here.
    bool IsValidEntrant(Collider other, out string label)
    {
        if (other.GetComponentInParent<CarController>() != null)
        {
            label = "car";
            return true;
        }

        // Assumption: your player object/collider is tagged "Player". If it isn't, either
        // add that tag, or swap this for a component check like the CarController one above
        // (e.g. other.GetComponentInParent<YourPlayerScript>() != null).
        if (other.CompareTag("Player"))
        {
            label = "player";
            return true;
        }

        label = null;
        return false;
    }

    void LoadLevel(string label)
    {
        Debug.Log($"[TimeMachinePortal] {label} entered \"{gameObject.name}\" \u2192 loading: {destinationLevel}");

        if (string.IsNullOrEmpty(destinationLevel))
        {
            Debug.LogWarning($"[TimeMachinePortal] \"{gameObject.name}\" has no destinationLevel set - not loading anything.");
            return;
        }

        // destinationLevel must exactly match a scene name that's added to
        // File > Build Settings > Scenes In Build, or this throws at runtime.
        SceneManager.LoadScene(destinationLevel);
    }
}