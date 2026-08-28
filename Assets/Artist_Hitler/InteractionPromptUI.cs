using UnityEngine;
using TMPro;



public class InteractionPromptUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("The painting this prompt is attached to")]
    public PaintingCanvas painting;

    [Header("UI")]
    [Tooltip("The TextMeshPro text element that shows the prompt")]
    public TMP_Text promptText;

    [Tooltip("Message shown while in range and free to interact")]
    public string promptMessage = "Press F to Edit Painting";

    private void Start()
    {
        if (painting == null)
        {
            Debug.LogWarning(
                "[InteractionPromptUI] No PaintingCanvas assigned - the prompt will never show."
            );
        }

        if (promptText == null)
        {
            Debug.LogWarning(
                "[InteractionPromptUI] No TextMeshPro promptText assigned - nothing will render."
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

        
        promptText.enabled = painting.CanInteract;
    }
}