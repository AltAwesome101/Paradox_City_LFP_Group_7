using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    // =========================================================
    // TARGETS
    // =========================================================

    [Header("===== TARGETS =====")]

    [Tooltip("The player the minimap normally follows.")]
    public Transform player;

    [Tooltip("The car the minimap should follow while driving.")]
    public CarController car;


    // =========================================================
    // CAMERA
    // =========================================================

    [Header("===== CAMERA =====")]

    [Tooltip("Height of the minimap camera above the world.")]
    public float cameraHeight = 30f;

    [Tooltip("If enabled, camera movement smoothly follows the target.")]
    public bool smoothFollow = false;

    [Tooltip("How quickly the camera follows the target.")]
    public float followSpeed = 15f;


    // =========================================================
    // ROTATION
    // =========================================================

    [Header("===== ROTATION =====")]

    [Tooltip("Rotate the minimap with the player/car.")]
    public bool rotateWithTarget = false;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("===== DEBUG =====")]

    public bool debugLogs = true;


    // =========================================================
    // PRIVATE
    // =========================================================

    private Transform currentTarget;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        /*
         * Start by following the player.
         */
        if (player != null)
        {
            currentTarget = player;

            if (debugLogs)
            {
                Debug.Log(
                    "[MINIMAP] Starting by following PLAYER."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[MINIMAP] Player has not been assigned!"
            );
        }
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        UpdateTarget();

        FollowTarget();
    }


    // =========================================================
    // TARGET SELECTION
    // =========================================================

    private void UpdateTarget()
    {
        /*
         * If the car exists and is currently being driven,
         * the minimap follows the car.
         */
        if (
            car != null &&
            car.isBeingDriven)
        {
            if (currentTarget != car.transform)
            {
                currentTarget =
                    car.transform;

                if (debugLogs)
                {
                    Debug.Log(
                        "[MINIMAP] CAR IS BEING DRIVEN -> " +
                        "Following CAR."
                    );
                }
            }

            return;
        }


        /*
         * Otherwise follow the player.
         */
        if (
            player != null &&
            currentTarget != player)
        {
            currentTarget =
                player;

            if (debugLogs)
            {
                Debug.Log(
                    "[MINIMAP] CAR NOT BEING DRIVEN -> " +
                    "Following PLAYER."
                );
            }
        }
    }


    // =========================================================
    // FOLLOW
    // =========================================================

    private void FollowTarget()
    {
        if (currentTarget == null)
            return;


        Vector3 targetPosition =
            new Vector3(
                currentTarget.position.x,
                cameraHeight,
                currentTarget.position.z
            );


        // =====================================================
        // POSITION
        // =====================================================

        if (smoothFollow)
        {
            transform.position =
                Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    followSpeed *
                    Time.deltaTime
                );
        }
        else
        {
            transform.position =
                targetPosition;
        }


        // =====================================================
        // ROTATION
        // =====================================================

        if (rotateWithTarget)
        {
            transform.rotation =
                Quaternion.Euler(
                    90f,
                    currentTarget.eulerAngles.y,
                    0f
                );
        }
        else
        {
            /*
             * North-up minimap.
             */
            transform.rotation =
                Quaternion.Euler(
                    90f,
                    0f,
                    0f
                );
        }
    }
}