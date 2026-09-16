using UnityEngine;
using TMPro;
public class InteractionSystem : MonoBehaviour
{
    [Header("Interaction")]
    public Camera playerCamera;
    public float interactionDistance = 3f;

    [Header("UI")]
    public GameObject interactionTextObject;
    public TextMeshProUGUI interactionText;

    private void Update()
    {
        CheckForInteraction();
    }

    private void CheckForInteraction()
    {
        // Shoot a ray from the centre of the screen
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        // Check if the ray hits something
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Check if we hit an NPC
            HistoricalFigure npc =
                hit.collider.GetComponent<HistoricalFigure>();

            if (npc != null)
            {
                ShowInteraction("[E] Talk");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    TalkToNPC(npc);
                }

                return;
            }
        }

        // Nothing interactable
        HideInteraction();
    }

    private void TalkToNPC(HistoricalFigure npc)
    {
        Debug.Log("Talking to the historical figure.");

        npc.StartConversation();
    }

    private void ShowInteraction(string message)
    {
        if (interactionTextObject != null)
        {
            interactionTextObject.SetActive(true);
        }

        if (interactionText != null)
        {
            interactionText.text = message;
        }
    }

    private void HideInteraction()
    {
        if (interactionTextObject != null)
        {
            interactionTextObject.SetActive(false);
        }
    }
}