using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Drag the specific PaintingCanvas this prompt belongs to.")]
    public PaintingCanvas painting;

    [Header("UI")]
    [Tooltip("The TextMeshPro text element that shows the prompt.")]
    public TMP_Text promptText;

    [Tooltip("Message shown while in range and free to interact.")]
    public string promptMessage = "Press F to Edit Painting";

    [Header("Debug")]
    public bool debugLogging = false;

    [Tooltip("How often (seconds) to print the status line while debugLogging is on.")]
    public float debugPrintInterval = 1f;

    private float debugTimer;
    private bool lastEnabledState;

    private void Start()
    {
        if (painting == null)
        {
            Debug.LogWarning(
                $"[InteractionPromptUI] ({gameObject.name}) No PaintingCanvas assigned - " +
                "drag the correct PaintingCanvas into the Painting field."
            );
        }

        if (promptText == null)
        {
            Debug.LogWarning(
                $"[InteractionPromptUI] ({gameObject.name}) No TextMeshPro promptText assigned - nothing will render."
            );
        }
        else
        {
            promptText.text = promptMessage;
            promptText.enabled = false;
        }
    }

    private void Update()
    {
        if (painting == null || promptText == null)
            return;

        
        bool shouldShow = painting.CanInteract && !painting.IsComplete;

        promptText.enabled = shouldShow;

        if (debugLogging)
        {
            
            if (promptText.enabled != lastEnabledState)
            {
                lastEnabledState = promptText.enabled;

                Debug.Log(
                    $"[InteractionPromptUI] ({gameObject.name} -> {painting.name}) " +
                    $"prompt now {(promptText.enabled ? "SHOWING" : "hidden")}."
                );
            }

            
            debugTimer += Time.deltaTime;

            if (debugTimer >= debugPrintInterval)
            {
                debugTimer = 0f;

                Debug.Log(
                    $"[InteractionPromptUI] ({gameObject.name} -> {painting.name}) " +
                    $"CanInteract={painting.CanInteract} | " +
                    $"IsComplete={painting.IsComplete} | " +
                    $"IsInteracting={painting.IsInteracting}"
                );
            }
        }
    }
}