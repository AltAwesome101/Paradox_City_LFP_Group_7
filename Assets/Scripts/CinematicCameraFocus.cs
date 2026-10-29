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

        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        if (!focusingOnNPC || targetNPC == null)
            return;

        // Keep updating the camera pose while Gandhi moves.
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

        // Do not overwrite the original player camera pose
        // if a cinematic is already running.
        if (!focusingOnNPC)
        {
            originalParent = transform.parent;
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
        }

        targetNPC = npc;
        followTargetDuringFocus = followMovingTarget;

        CalculateCinematicPose();

        if (playerController != null)
        {
            playerController.enabled = false;
        }
        else
        {
            Debug.LogWarning(
                "[CinematicCameraFocus] No FPController assigned."
            );
        }

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

        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }

        if (playerController != null)
            playerController.enabled = true;

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
        if (playerController != null)
            playerController.enabled = true;
    }
}