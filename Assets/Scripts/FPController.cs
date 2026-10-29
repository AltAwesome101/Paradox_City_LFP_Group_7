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

    [Header("Glass Pickup and Drop-off")]
    public Transform glassHoldPoint;
    public GameObject mainCounter;
    public Transform glassDropOffPoint;
    public Transform[] glassDropOffSlots;
    public TextMeshProUGUI glassInteractionPrompt;

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
    private CleanableGlass carriedGlass;
    private int nextGlassSlotIndex = 0;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform != null)
        {
            verticalRotation = cameraTransform.localEulerAngles.x;

            if (verticalRotation > 180f)
                verticalRotation -= 360f;
        }

        HidePickupPrompt();
        HideHandInBeerPrompt();
        HideGlassPrompt();

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
            HideGlassPrompt();
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
            return;

        // Preserve the existing beer-serving behaviour.
        if (carriedBeer != null)
        {
            TryServeBeer();
            return;
        }

        // If carrying a glass, E attempts to return it to the counter.
        if (carriedGlass != null)
        {
            TryDropOffGlass();
            return;
        }

        // Otherwise, try a glass first, then a beer.
        TryPickUpGlassOrBeer();
    }

    private void HandleMovement()
    {
        if (controller == null || !controller.enabled ||
            cameraTransform == null)
            return;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement =
            forward * moveInput.y + right * moveInput.x;

        if (movement.magnitude > 1f)
            movement.Normalize();

        controller.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void HandleLook()
    {
        if (cameraTransform == null)
            return;

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
            return;

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private Camera GetPlayerCamera()
    {
        return cameraTransform != null
            ? cameraTransform.GetComponent<Camera>()
            : null;
    }

    private bool TryGetLookHit(out RaycastHit hit)
    {
        hit = default;

        Camera cam = GetPlayerCamera();

        if (cam == null)
            return false;

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        return Physics.Raycast(ray, out hit, interactionRange);
    }

    private void UpdateInteractionPrompts()
    {
        HidePickupPrompt();
        HideHandInBeerPrompt();
        HideGlassPrompt();

        if (!TryGetLookHit(out RaycastHit hit))
            return;

        // Carrying beer: show the existing hand-in prompt.
        if (carriedBeer != null)
        {
            HistoricalFigure npc =
                hit.collider.GetComponentInParent<HistoricalFigure>();

            if (npc != null && npc.HasOrdered())
                ShowHandInBeerPrompt();

            return;
        }

        // Carrying a glass: show the return prompt when aiming
        // at the main counter.
        if (carriedGlass != null)
        {
            if (IsMainCounterHit(hit))
                ShowGlassPrompt("Press E to place glass");

            return;
        }

        // Glass pickup is available before beer preparation.
        CleanableGlass glass =
            hit.collider.GetComponentInParent<CleanableGlass>();

        if (glass != null &&
            !glass.isCollected &&
            !glass.isReturned)
        {
            ShowGlassPrompt("Press E to pick up glass");
            return;
        }

        // Preserve the existing beer preparation requirement.
        if (preparationMinigame == null ||
            !preparationMinigame.IsPreparationComplete())
            return;

        Beer beer = hit.collider.GetComponentInParent<Beer>();

        if (beer != null &&
            preparationMinigame.CanPickUpBeer(beer))
        {
            ShowPickupPrompt();
        }
    }

    private bool IsMainCounterHit(RaycastHit hit)
    {
        if (mainCounter == null)
            return false;

        Transform hitTransform = hit.collider.transform;
        Transform counterTransform = mainCounter.transform;

        return hitTransform == counterTransform ||
               hitTransform.IsChildOf(counterTransform);
    }

    private void TryPickUpGlassOrBeer()
    {
        if (!TryGetLookHit(out RaycastHit hit))
            return;

        // Try to pick up a glass first.
        CleanableGlass glass =
            hit.collider.GetComponentInParent<CleanableGlass>();

        if (glass != null &&
            !glass.isCollected &&
            !glass.isReturned)
        {
            Transform holdPoint = glassHoldPoint != null
                ? glassHoldPoint
                : beerHoldPoint;

            if (holdPoint == null)
            {
                Debug.LogError(
                    "FPController: Assign Glass Hold Point " +
                    "or Beer Hold Point in the Inspector."
                );
                return;
            }

            glass.PickUp(holdPoint);
            carriedGlass = glass;

            HideGlassPrompt();

            Debug.Log("Picked up glass: " + glass.glassName);
            return;
        }

        // If the object is not a glass, try the existing beer logic.
        TryPickUpBeer();
    }

    private void TryDropOffGlass()
    {
        if (carriedGlass == null)
            return;

        if (!TryGetLookHit(out RaycastHit hit))
            return;

        if (!IsMainCounterHit(hit))
            return;

        Transform dropPoint = glassDropOffPoint;

        // Use individual slots if you have assigned them.
        if (glassDropOffSlots != null &&
            nextGlassSlotIndex < glassDropOffSlots.Length &&
            glassDropOffSlots[nextGlassSlotIndex] != null)
        {
            dropPoint = glassDropOffSlots[nextGlassSlotIndex];
        }

        if (dropPoint == null)
        {
            Debug.LogError(
                "FPController: Assign Glass Drop Off Point " +
                "in the Inspector."
            );
            return;
        }

        CleanableGlass glassToReturn = carriedGlass;
        glassToReturn.ReturnToCounter(dropPoint);

        carriedGlass = null;
        nextGlassSlotIndex++;

        HideGlassPrompt();

        Debug.Log("Returned glass: " + glassToReturn.glassName);
    }

    private void TryPickUpBeer()
    {
        if (carriedBeer != null)
            return;

        if (preparationMinigame == null ||
            !preparationMinigame.IsPreparationComplete())
        {
            Debug.Log("Prepare the beer before picking one up.");
            return;
        }

        if (!TryGetLookHit(out RaycastHit hit))
            return;

        Beer beer = hit.collider.GetComponentInParent<Beer>();

        if (beer == null)
            return;

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
            beerCollider.enabled = false;

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
        if (carriedBeer == null)
            return;

        if (!TryGetLookHit(out RaycastHit hit))
            return;

        HistoricalFigure npc =
            hit.collider.GetComponentInParent<HistoricalFigure>();

        if (npc == null || !npc.HasOrdered())
            return;

        Beer beerToServe = carriedBeer;

        npc.ServeBeer(beerToServe);

        beerToServe.gameObject.SetActive(false);
        carriedBeer = null;

        HidePickupPrompt();
        HideHandInBeerPrompt();
    }

    private void ShowPickupPrompt()
    {
        if (pickupPrompt != null)
            pickupPrompt.gameObject.SetActive(true);
    }

    private void HidePickupPrompt()
    {
        if (pickupPrompt != null)
            pickupPrompt.gameObject.SetActive(false);
    }

    private void ShowHandInBeerPrompt()
    {
        if (handInBeerPrompt != null)
            handInBeerPrompt.gameObject.SetActive(true);
    }

    private void HideHandInBeerPrompt()
    {
        if (handInBeerPrompt != null)
            handInBeerPrompt.gameObject.SetActive(false);
    }

    private void ShowGlassPrompt(string message)
    {
        if (glassInteractionPrompt == null)
            return;

        glassInteractionPrompt.text = message;
        glassInteractionPrompt.gameObject.SetActive(true);
    }

    private void HideGlassPrompt()
    {
        if (glassInteractionPrompt != null)
            glassInteractionPrompt.gameObject.SetActive(false);
    }

    public bool IsCarryingBeer()
    {
        return carriedBeer != null;
    }

    public Beer GetCarriedBeer()
    {
        return carriedBeer;
    }

    public bool IsCarryingGlass()
    {
        return carriedGlass != null;
    }
}