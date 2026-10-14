using UnityEngine;

public class CinematicCameraFocus : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Focus Settings")]
    public float focusDistance = 4f;
    public float focusHeight = 2f;
    public float transitionSpeed = 4f;

    private Transform player;
    private Transform targetNPC;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private bool focusingOnNPC = false;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera =
                GetComponent<Camera>();
        }

        originalLocalPosition =
            transform.localPosition;

        originalLocalRotation =
            transform.localRotation;

        player =
            transform.parent;
    }

    private void LateUpdate()
    {
        if (!focusingOnNPC)
        {
            return;
        }

        if (targetNPC == null)
        {
            return;
        }

        Vector3 targetPosition =
            targetNPC.position -
            targetNPC.forward *
            focusDistance;

        targetPosition.y +=
            focusHeight;

        transform.position =
            Vector3.Lerp(
                transform.position,
                targetPosition,
                transitionSpeed *
                Time.unscaledDeltaTime
            );

        Vector3 lookDirection =
            targetNPC.position +
            Vector3.up *
            1.2f -
            transform.position;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    lookDirection
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    transitionSpeed *
                    Time.unscaledDeltaTime
                );
        }
    }

    public void FocusOnNPC(
        Transform npc
    )
    {
        if (npc == null)
        {
            return;
        }

        targetNPC = npc;
        focusingOnNPC = true;

        Debug.Log(
            "Cinematic camera focusing on NPC."
        );
    }

    public void ReturnToPlayer()
    {
        focusingOnNPC = false;
        targetNPC = null;

        if (player != null)
        {
            transform.localPosition =
                originalLocalPosition;

            transform.localRotation =
                originalLocalRotation;
        }

        Debug.Log(
            "Cinematic camera returned to player."
        );
    }
}