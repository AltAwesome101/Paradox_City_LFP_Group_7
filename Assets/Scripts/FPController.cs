using UnityEngine;
using UnityEngine.InputSystem;

public class FPController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float acceleration = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public float lookSensitivity = 2f;
    public float verticalLookLimit = 70f;

    [Header("Interaction Settings")]
    public float interactionRange = 3f;

    [Header("Beer Pickup")]
    public Transform beerHoldPoint;

    [Header("Interaction UI")]
    public GameObject interactionTextObject;
    public TMPro.TextMeshProUGUI interactionText;

    private CharacterController controller;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private Vector3 velocity;
    private Vector3 currentMoveVelocity;

    private float verticalRotation = 0f;

    private bool isSprinting;

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
        HandleInteraction();
    }

    // ================= INPUT =================

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started &&
            controller.isGrounded)
        {
            velocity.y =
                Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            TryInteract();
        }
    }

    // ================= MOVEMENT =================

    private void HandleMovement()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 targetMove =
            forward * moveInput.y +
            right * moveInput.x;

        targetMove.Normalize();

        float currentSpeed =
            isSprinting
                ? sprintSpeed
                : moveSpeed;

        Vector3 targetVelocity =
            targetMove * currentSpeed;

        currentMoveVelocity = Vector3.Lerp(
            currentMoveVelocity,
            targetVelocity,
            acceleration * Time.deltaTime
        );

        controller.Move(
            currentMoveVelocity *
            Time.deltaTime
        );
    }

    // ================= LOOK =================

    private void HandleLook()
    {
        float mouseX =
            lookInput.x * lookSensitivity;

        float mouseY =
            lookInput.y * lookSensitivity;

        verticalRotation -= mouseY;

        verticalRotation = Mathf.Clamp(
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
            Vector3.up * mouseX
        );
    }

    // ================= GRAVITY =================

    private void HandleGravity()
    {
        if (controller.isGrounded &&
            velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y +=
            gravity * Time.deltaTime;

        controller.Move(
            velocity *
            Time.deltaTime
        );
    }

    // ================= INTERACTION =================

    private void HandleInteraction()
    {
        if (carriedBeer != null)
        {
            HideInteractionText();
            return;
        }

        Camera cam =
            cameraTransform.GetComponent<Camera>();

        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(
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
                ShowInteractionText(
                    "[E] Pick Up Beer"
                );

                return;
            }
        }

        HideInteractionText();
    }

    // ================= INTERACT =================

    private void TryInteract()
    {
        Camera cam =
            cameraTransform.GetComponent<Camera>();

        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(
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

    // ================= BEER PICKUP =================

    private void PickUpBeer(Beer beer)
    {
        if (carriedBeer != null)
            return;

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

        HideInteractionText();
    }

    // ================= UI =================

    private void ShowInteractionText(
        string message
    )
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

    private void HideInteractionText()
    {
        if (interactionTextObject != null)
        {
            interactionTextObject.SetActive(false);
        }
    }

    // ================= BEER INFO =================

    public bool IsCarryingBeer()
    {
        return carriedBeer != null;
    }

    public Beer GetCarriedBeer()
    {
        return carriedBeer;
    }
}
