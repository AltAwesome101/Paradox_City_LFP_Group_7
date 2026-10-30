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

    [Tooltip("Height of the camera relative to the NPC.")]
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
    private bool followTargetDuringFocus;

    // Player restoration information.
    private Transform playerTransform;
    private CharacterController playerCharacterController;

    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;

    private bool originalControllerEnabled;

    private Renderer[] playerRenderers;
    private bool[] originalRendererStates;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera != null &&
            playerCamera.transform != transform)
        {
            Debug.LogWarning(
                "[CinematicCameraFocus] Attach this script " +
                "to the actual player camera."
            );
        }

        if (playerController == null)
            playerController = GetComponentInParent<FPController>();

        if (playerController != null)
        {
            playerTransform = playerController.transform;

            playerCharacterController =
                playerTransform.GetComponent<CharacterController>();

            playerRenderers =
                playerTransform.GetComponentsInChildren<Renderer>(true);

            originalRendererStates = new bool[playerRenderers.Length];

            for (int i = 0; i < playerRenderers.Length; i++)
            {
                originalRendererStates[i] = playerRenderers[i].enabled;
            }
        }

        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        if (!focusingOnNPC || targetNPC == null)
            return;

        if (followTargetDuringFocus)
            CalculateCinematicPose();

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

            if (
                Vector3.Distance(
                    transform.position,
                    cinematicPosition
                ) < 0.02f &&
                Quaternion.Angle(
                    transform.rotation,
                    cinematicRotation
                ) < 0.5f
            )
            {
                transform.position = cinematicPosition;
                transform.rotation = cinematicRotation;
                transitioning = false;
            }
        }
        else
        {
            transform.position = cinematicPosition;
            transform.rotation = cinematicRotation;
        }
    }

    private void CalculateCinematicPose()
    {
        if (targetNPC == null)
            return;

        cinematicPosition =
            targetNPC.position -
            targetNPC.forward * focusDistance +
            Vector3.up * focusHeight;

        Vector3 lookTarget =
            targetNPC.position +
            Vector3.up * lookAtHeight;

        Vector3 lookDirection =
            lookTarget - cinematicPosition;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            cinematicRotation =
                Quaternion.LookRotation(lookDirection);
        }
    }

    // Save the player's position and hide the player.
    private void HidePlayer()
    {
        if (playerTransform == null)
            return;

        originalPlayerPosition = playerTransform.position;
        originalPlayerRotation = playerTransform.rotation;

        if (playerCharacterController != null)
        {
            originalControllerEnabled =
                playerCharacterController.enabled;

            playerCharacterController.enabled = false;
        }

        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                playerRenderers[i].enabled = false;
            }
        }

        Debug.Log("[CinematicCameraFocus] Player hidden.");
    }

    // Restore the player exactly where they were.
    private void RestorePlayer()
    {
        if (playerTransform == null)
            return;

        // Restore the original world position and rotation.
        playerTransform.SetPositionAndRotation(
            originalPlayerPosition,
            originalPlayerRotation
        );

        // Restore the player's visual components.
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null)
            {
                playerRenderers[i].enabled =
                    originalRendererStates[i];
            }
        }

        // Re-enable the CharacterController only if it
        // was enabled before the cinematic.
        if (playerCharacterController != null)
        {
            playerCharacterController.enabled =
                originalControllerEnabled;
        }

        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }

        Debug.Log("[CinematicCameraFocus] Player restored.");
    }

    // CINEMATIC 1: Gandhi enters and walks to his seat.
    public void FocusOnEntrance(Transform npc)
    {
        FocusOnNPC(npc, true);
    }

    // CINEMATIC 2: Gandhi drinks the beer.
    public void FocusOnDrinking(Transform npc)
    {
        FocusOnNPC(npc, true);
    }

    // Keeps compatibility with existing script calls.
    public void FocusOnNPC(Transform npc)
    {
        FocusOnNPC(npc, false);
    }

    public void FocusOnNPC(
        Transform npc,
        bool followMovingTarget
    )
    {
        if (npc == null)
            return;

        if (!focusingOnNPC)
        {
            originalParent = transform.parent;
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;

            HidePlayer();
        }

        targetNPC = npc;
        followTargetDuringFocus = followMovingTarget;

        CalculateCinematicPose();

        transform.SetParent(null, true);

        focusingOnNPC = true;
        transitioning = true;

        Debug.Log(
            "[CinematicCameraFocus] Cinematic started. " +
            "Following NPC: " + followMovingTarget
        );
    }

    public void ReturnToPlayer()
    {
        if (!focusingOnNPC)
            return;

        focusingOnNPC = false;
        transitioning = false;
        followTargetDuringFocus = false;
        targetNPC = null;

        // Restore the player's original position first.
        RestorePlayer();

        // Return the camera to its original parent.
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }

        Debug.Log(
            "[CinematicCameraFocus] Camera returned to player."
        );
    }

    public bool IsFocusingOnNPC()
    {
        return focusingOnNPC;
    }

    private void OnDestroy()
    {
        if (focusingOnNPC)
        {
            RestorePlayer();
        }
    }
}