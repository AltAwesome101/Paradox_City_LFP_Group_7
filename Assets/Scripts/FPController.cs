using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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

    [Header("Interaction Prompts")]
    public TextMeshProUGUI pickupPrompt;
    public TextMeshProUGUI handInBeerPrompt;

    [Header("Beer Preparation")]
    public PreparationMinigame preparationMinigame;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private float verticalRotation = 0f;
    private Beer carriedBeer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform != null)
        {
            verticalRotation = cameraTransform.localEulerAngles.x;

            if (verticalRotation > 180f)
            {
                verticalRotation -= 360f;
            }
        }

        HidePickupPrompt();
        HideHandInBeerPrompt();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (IsPreparationActive())
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            velocity.x = 0f;
            velocity.z = 0f;

            HidePickupPrompt();
            HideHandInBeerPrompt();
            return;
        }

        HandleMovement();
        HandleLook();
        HandleGravity();
        UpdateInteractionPrompts();
    }

    private bool IsPreparationActive()
    {
        return preparationMinigame != null &&
               preparationMinigame.IsMinigameRunning();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (IsPreparationActive())
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (IsPreparationActive())
        {
            lookInput = Vector2.zero;
            return;
        }

        lookInput = context.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (IsPreparationActive() || !context.started)
        {
            return;
        }

        if (carriedBeer != null)
        {
            TryServeBeer();
        }
        else
        {
            TryPickUpBeer();
        }
    }

    private void HandleMovement()
    {
        if (controller == null || !controller.enabled ||
            cameraTransform == null)
        {
            return;
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement =
            forward * moveInput.y + right * moveInput.x;

        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        controller.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void HandleLook()
    {
        if (cameraTransform == null)
        {
            return;
        }

        float mouseX = lookInput.x * lookSensitivity;
        float mouseY = lookInput.y * lookSensitivity;

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(
            verticalRotation,
            -verticalLookLimit,
            verticalLookLimit
        );

        cameraTransform.localRotation =
            Quaternion.Euler(verticalRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleGravity()
    {
        if (controller == null || !controller.enabled)
        {
            return;
        }

        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateInteractionPrompts()
    {
        HidePickupPrompt();
        HideHandInBeerPrompt();

        if (preparationMinigame == null ||
            !preparationMinigame.IsPreparationComplete() ||
            cameraTransform == null)
        {
            return;
        }

        Camera cam = cameraTransform.GetComponent<Camera>();

        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(
            ray, out RaycastHit hit, interactionRange))
        {
            return;
        }

        // If the player is carrying beer, show the hand-in prompt
        // when aiming at an NPC who has placed an order.
        if (carriedBeer != null)
        {
            HistoricalFigure npc =
                hit.collider.GetComponentInParent<HistoricalFigure>();

            if (npc != null && npc.HasOrdered())
            {
                ShowHandInBeerPrompt();
            }

            return;
        }

        // If the player is not carrying beer, show the pickup prompt
        // only for the beer assigned by the preparation minigame.
        Beer beer = hit.collider.GetComponentInParent<Beer>();

        if (beer != null &&
            preparationMinigame.CanPickUpBeer(beer))
        {
            ShowPickupPrompt();
        }
    }

    private void ShowPickupPrompt()
    {
        if (pickupPrompt != null &&
            !pickupPrompt.gameObject.activeSelf)
        {
            pickupPrompt.gameObject.SetActive(true);
        }
    }

    private void HidePickupPrompt()
    {
        if (pickupPrompt != null &&
            pickupPrompt.gameObject.activeSelf)
        {
            pickupPrompt.gameObject.SetActive(false);
        }
    }

    private void ShowHandInBeerPrompt()
    {
        if (handInBeerPrompt != null &&
            !handInBeerPrompt.gameObject.activeSelf)
        {
            handInBeerPrompt.gameObject.SetActive(true);
        }
    }

    private void HideHandInBeerPrompt()
    {
        if (handInBeerPrompt != null &&
            handInBeerPrompt.gameObject.activeSelf)
        {
            handInBeerPrompt.gameObject.SetActive(false);
        }
    }

    private void TryPickUpBeer()
    {
        if (carriedBeer != null)
        {
            return;
        }

        if (preparationMinigame == null ||
            !preparationMinigame.IsPreparationComplete())
        {
            Debug.Log("Prepare the beer before picking one up.");
            return;
        }

        Camera cam = cameraTransform != null
            ? cameraTransform.GetComponent<Camera>()
            : null;

        if (cam == null)
        {
            Debug.LogError(
                "FPController: No Camera component on cameraTransform."
            );
            return;
        }

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
        {
            Beer beer = hit.collider.GetComponentInParent<Beer>();

            if (beer != null)
            {
                if (!preparationMinigame.CanPickUpBeer(beer))
                {
                    Debug.Log(
                        "That beer is not assigned to you. " +
                        "Find the assigned beer."
                    );
                    return;
                }

                PickUpBeer(beer);
                HidePickupPrompt();
            }
        }
    }

    private void PickUpBeer(Beer beer)
    {
        if (beerHoldPoint == null)
        {
            Debug.LogError("Beer Hold Point is not assigned!");
            return;
        }

        carriedBeer = beer;

        beer.transform.SetParent(beerHoldPoint);
        beer.transform.localPosition = Vector3.zero;
        beer.transform.localRotation = Quaternion.identity;

        Collider[] beerColliders =
            beer.GetComponentsInChildren<Collider>();

        foreach (Collider beerCollider in beerColliders)
        {
            beerCollider.enabled = false;
        }

        Rigidbody beerRigidbody = beer.GetComponent<Rigidbody>();

        if (beerRigidbody != null)
        {
            beerRigidbody.isKinematic = true;
            beerRigidbody.useGravity = false;
        }

        Debug.Log("Picked up: " + beer.beerName);
    }

    private void TryServeBeer()
    {
        if (carriedBeer == null || cameraTransform == null)
        {
            return;
        }

        Camera cam = cameraTransform.GetComponent<Camera>();

        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
        {
            HistoricalFigure npc =
                hit.collider.GetComponentInParent<HistoricalFigure>();

            if (npc != null && npc.HasOrdered())
            {
                Beer beerToServe = carriedBeer;

                npc.ServeBeer(beerToServe);

                beerToServe.gameObject.SetActive(false);
                carriedBeer = null;

                HidePickupPrompt();
                HideHandInBeerPrompt();
            }
        }
    }

    public bool IsCarryingBeer()
    {
        return carriedBeer != null;
    }

    public Beer GetCarriedBeer()
    {
        return carriedBeer;
    }
}