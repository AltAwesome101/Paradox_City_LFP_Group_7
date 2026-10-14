using UnityEngine;

public class CinematicCameraFocus : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Player Controller")]
    public FPController playerController;

    [Header("Focus Settings")]
    [Tooltip("Distance between the camera and the NPC.")]
    public float focusDistance = 4f;

    [Tooltip("Height of the camera relative to the NPC's position.")]
    public float focusHeight = 2f;

    [Tooltip("Speed of the transition into the cinematic.")]
    public float transitionSpeed = 4f;

    [Header("Focus Target")]
    [Tooltip("Height above the NPC's position that the camera looks at.")]
    public float lookAtHeight = 1.2f;

    private Transform targetNPC;
    private Transform originalParent;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Vector3 cinematicPosition;
    private Quaternion cinematicRotation;

    private bool focusingOnNPC;
    private bool transitioning;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null && playerCamera.transform != transform)
        {
            Debug.LogWarning(
                "[CinematicCameraFocus] The script should be attached " +
                "to the actual player camera."
            );
        }

        // Find the first-person controller before detaching the camera.
        if (playerController == null)
            playerController = GetComponentInParent<FPController>();

        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        if (!focusingOnNPC || targetNPC == null)
            return;

        // Smoothly move into the cinematic position.
        if (transitioning)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                cinematicPosition,
                transitionSpeed * Time.unscaledDeltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                cinematicRotation,
                transitionSpeed * Time.unscaledDeltaTime
            );

            // Finish the transition when sufficiently close.
            if (Vector3.Distance(
                    transform.position,
                    cinematicPosition) < 0.02f &&
                Quaternion.Angle(
                    transform.rotation,
                    cinematicRotation) < 0.5f)
            {
                transform.position = cinematicPosition;
                transform.rotation = cinematicRotation;
                transitioning = false;
            }
        }
        else
        {
            // Keep the camera fixed on the cinematic view.
            // Mouse movement and player rotation cannot override it.
            transform.position = cinematicPosition;
            transform.rotation = cinematicRotation;
        }
    }

    public void FocusOnNPC(Transform npc)
    {
        if (npc == null)
            return;

        targetNPC = npc;

        // Calculate a fixed cinematic camera position.
        cinematicPosition =
            targetNPC.position -
            targetNPC.forward * focusDistance +
            Vector3.up * focusHeight;

        // Calculate the exact rotation required to look at the NPC.
        Vector3 lookTarget =
            targetNPC.position + Vector3.up * lookAtHeight;

        Vector3 lookDirection =
            lookTarget - cinematicPosition;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            cinematicRotation =
                Quaternion.LookRotation(lookDirection);
        }

        // Prevent the player from moving or rotating the camera.
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        else
        {
            Debug.LogWarning(
                "[CinematicCameraFocus] No FPController assigned. " +
                "The cinematic camera may be affected by player controls."
            );
        }

        // Detach the camera so player rotation and movement
        // cannot change its transform during the cinematic.
        transform.SetParent(null, true);

        focusingOnNPC = true;
        transitioning = true;

        Debug.Log(
            "[CinematicCameraFocus] Cinematic started. " +
            "Player controls disabled."
        );
    }

    public void ReturnToPlayer()
    {
        if (!focusingOnNPC)
            return;

        focusingOnNPC = false;
        transitioning = false;
        targetNPC = null;

        // Restore the camera to its original player hierarchy.
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }

        // Restore normal first-person movement and mouse-look.
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        Debug.Log(
            "[CinematicCameraFocus] Camera returned to player. " +
            "Player controls restored."
        );
    }

    public bool IsFocusingOnNPC()
    {
        return focusingOnNPC;
    }

    private void OnDestroy()
    {
        // Avoid leaving the controller disabled if this component
        // is removed while the player is still in the scene.
        if (playerController != null)
            playerController.enabled = true;
    }
}