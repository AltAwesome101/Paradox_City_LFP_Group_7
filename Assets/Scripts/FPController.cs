using UnityEngine;
using UnityEngine.InputSystem;

public class FPController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Camera")]
    public Transform cameraTransform;
    public float lookSensitivity = 2f;
    public float verticalLookLimit = 70f;

    [Header("Beer Pickup")]
    public Transform beerHoldPoint;
    public float interactionRange = 3f;

    private CharacterController controller;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private Vector3 velocity;

    private float verticalRotation = 0f;

    private Beer carriedBeer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleLook();
        HandleGravity();
    }

    // =========================
    // INPUT
    // =========================

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (carriedBeer != null)
            {
                TryServeBeer();
            }
            else
            {
                TryPickUpBeer();
            }
        }
    }

    // =========================
    // MOVEMENT
    // =========================

    private void HandleMovement()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement =
            forward * moveInput.y +
            right * moveInput.x;

        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        controller.Move(
            movement *
            moveSpeed *
            Time.deltaTime
        );
    }

    // =========================
    // LOOK
    // =========================

    private void HandleLook()
    {
        float mouseX =
            lookInput.x *
            lookSensitivity;

        float mouseY =
            lookInput.y *
            lookSensitivity;

        verticalRotation -= mouseY;

        verticalRotation =
            Mathf.Clamp(
                verticalRotation,
                -verticalLookLimit,
                verticalLookLimit
            );

        cameraTransform.localRotation =
            Quaternion.Euler(
                verticalRotation,
                0f,
                0f
            );

        transform.Rotate(
            Vector3.up *
            mouseX
        );
    }

    // =========================
    // GRAVITY
    // =========================

    private void HandleGravity()
    {
        if (controller.isGrounded &&
            velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y +=
            gravity *
            Time.deltaTime;

        controller.Move(
            velocity *
            Time.deltaTime
        );
    }

    // =========================
    // BEER PICKUP
    // =========================

    private void TryPickUpBeer()
    {
        if (carriedBeer != null)
        {
            return;
        }

        Camera cam =
            cameraTransform.GetComponent<Camera>();

        if (cam == null)
        {
            return;
        }

        Ray ray =
            cam.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

        RaycastHit hit;

        if (Physics.Raycast(
            ray,
            out hit,
            interactionRange
        ))
        {
            Beer beer =
                hit.collider.GetComponent<Beer>();

            if (beer != null)
            {
                PickUpBeer(beer);
            }
        }
    }

    private void PickUpBeer(Beer beer)
    {
        if (beerHoldPoint == null)
        {
            Debug.LogError(
                "Beer Hold Point is not assigned!"
            );

            return;
        }

        carriedBeer = beer;

        beer.transform.SetParent(
            beerHoldPoint
        );

        beer.transform.localPosition =
            Vector3.zero;

        beer.transform.localRotation =
            Quaternion.identity;

        Collider beerCollider =
            beer.GetComponent<Collider>();

        if (beerCollider != null)
        {
            beerCollider.enabled = false;
        }

        Rigidbody beerRigidbody =
            beer.GetComponent<Rigidbody>();

        if (beerRigidbody != null)
        {
            beerRigidbody.isKinematic = true;
            beerRigidbody.useGravity = false;
        }

        Debug.Log(
            "Picked up: " +
            beer.beerName
        );
    }

    // =========================
    // SERVE BEER
    // =========================

    private void TryServeBeer()
    {
        if (carriedBeer == null)
        {
            return;
        }

        Camera cam =
            cameraTransform.GetComponent<Camera>();

        if (cam == null)
        {
            return;
        }

        Ray ray =
            cam.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

        RaycastHit hit;

        if (Physics.Raycast(
            ray,
            out hit,
            interactionRange
        ))
        {
            HistoricalFigure npc =
                hit.collider.GetComponent<HistoricalFigure>();

            if (npc != null &&
                npc.HasOrdered())
            {
                npc.ServeBeer(carriedBeer);

                carriedBeer.gameObject.SetActive(false);

                carriedBeer = null;
            }
        }
    }

    // =========================
    // BEER INFORMATION
    // =========================

    public bool IsCarryingBeer()
    {
        return carriedBeer != null;
    }

    public Beer GetCarriedBeer()
    {
        return carriedBeer;
    }
}