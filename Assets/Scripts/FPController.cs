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

    [Header("Glass Pickup")]
    public Transform glassHoldPoint;
    public TextMeshProUGUI glassPickupPrompt;
    public GameObject glassPickupPanel;

    [Header("Glass Drop-Off")]
    public Transform glassDropOffPoint;
    public float glassDropOffRange = 2f;
    public TextMeshProUGUI glassDropOffPrompt;
    public GameObject glassDropOffPanel;

    [Header("Beer Pickup UI")]
    public TextMeshProUGUI pickupPrompt;
    public GameObject pickupPanel;

    [Header("Beer Hand-In UI")]
    public TextMeshProUGUI handInBeerPrompt;
    public GameObject handInBeerPanel;

    [Header("Beer Preparation")]
    public PreparationMinigame preparationMinigame;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    private Beer carriedBeer;
    private CleanableGlass carriedGlass;

    // Controls player input without disabling this component.
    private bool inputEnabled = true;

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

        HideAllInteractionPrompts();

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

            HideAllInteractionPrompts();

            HandleGravity();
            return;
        }

        // Cinematic mode: block player controls but
        // keep gravity and grounding active.
        if (!inputEnabled)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;

            velocity.x = 0f;
            velocity.z = 0f;

            HideAllInteractionPrompts();

            HandleGravity();
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

    // Call this to block or restore player controls.
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        moveInput = Vector2.zero;
        lookInput = Vector2.zero;

        velocity.x = 0f;
        velocity.z = 0f;

        if (!enabled)
        {
            HideAllInteractionPrompts();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!inputEnabled || IsPreparationActive())
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!inputEnabled || IsPreparationActive())
        {
            lookInput = Vector2.zero;
            return;
        }

        lookInput = context.ReadValue<Vector2>();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!inputEnabled ||
            IsPreparationActive() ||
            !context.started)
        {
            return;
        }

        if (carriedBeer != null)
        {
            TryServeBeer();
            return;
        }

        if (carriedGlass != null)
        {
            TryReturnGlass();
            return;
        }

        TryPickUpObject();
    }

    private void HandleMovement()
    {
        if (controller == null ||
            !controller.enabled ||
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
            forward * moveInput.y +
            right * moveInput.x;

        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        controller.Move(
            movement * moveSpeed * Time.deltaTime
        );
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

    private Camera GetPlayerCamera()
    {
        if (cameraTransform == null)
        {
            return null;
        }

        return cameraTransform.GetComponent<Camera>();
    }

    private bool TryGetLookedAtObject(out RaycastHit hit)
    {
        hit = default;

        Camera cam = GetPlayerCamera();

        if (cam == null)
        {
            return false;
        }

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        return Physics.Raycast(
            ray,
            out hit,
            interactionRange
        );
    }

    private bool IsNearGlassDropOff()
    {
        if (glassDropOffPoint == null)
        {
            return false;
        }

        Vector3 playerPosition = transform.position;
        Vector3 dropOffPosition = glassDropOffPoint.position;

        playerPosition.y = 0f;
        dropOffPosition.y = 0f;

        return Vector3.Distance(
            playerPosition,
            dropOffPosition
        ) <= glassDropOffRange;
    }

    private void UpdateInteractionPrompts()
    {
        HideAllInteractionPrompts();

        if (carriedGlass != null)
        {
            if (IsNearGlassDropOff())
            {
                ShowGlassDropOffPrompt();
            }

            return;
        }

        if (!TryGetLookedAtObject(out RaycastHit hit))
        {
            return;
        }

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

        CleanableGlass glass =
            hit.collider.GetComponentInParent<CleanableGlass>();

        if (glass != null &&
            !glass.isCollected &&
            !glass.isReturned)
        {
            ShowGlassPickupPrompt();
            return;
        }

        if (preparationMinigame == null ||
            !preparationMinigame.IsPreparationComplete())
        {
            return;
        }

        Beer beer =
            hit.collider.GetComponentInParent<Beer>();

        if (beer != null &&
            preparationMinigame.CanPickUpBeer(beer))
        {
            ShowBeerPickupPrompt();
        }
    }

    private void TryPickUpObject()
    {
        if (carriedBeer != null || carriedGlass != null)
        {
            return;
        }

        if (!TryGetLookedAtObject(out RaycastHit hit))
        {
            return;
        }

        CleanableGlass glass =
            hit.collider.GetComponentInParent<CleanableGlass>();

        if (glass != null)
        {
            TryPickUpGlass(glass);
            return;
        }

        TryPickUpBeer(hit);
    }

    private void TryPickUpGlass(CleanableGlass glass)
    {
        if (glass == null ||
            glass.isCollected ||
            glass.isReturned)
        {
            return;
        }

        Transform holdPoint = glassHoldPoint != null
            ? glassHoldPoint
            : beerHoldPoint;

        if (holdPoint == null)
        {
            Debug.LogError(
                "FPController: Assign Glass Hold Point or Beer Hold Point."
            );
            return;
        }

        glass.PickUp(holdPoint);

        if (glass.isCollected)
        {
            carriedGlass = glass;
            HideAllInteractionPrompts();

            Debug.Log("Picked up glass: " + glass.glassName);
        }
    }

    private void TryReturnGlass()
    {
        if (carriedGlass == null)
        {
            return;
        }

        if (glassDropOffPoint == null)
        {
            Debug.LogError(
                "FPController: Glass Drop Off Point is not assigned."
            );
            return;
        }

        if (!IsNearGlassDropOff())
        {
            Debug.Log(
                "Move closer to the counter to return the glass."
            );
            return;
        }

        CleanableGlass glassToReturn = carriedGlass;

        glassToReturn.ReturnToCounter(glassDropOffPoint);

        if (glassToReturn.isReturned)
        {
            carriedGlass = null;
            HideAllInteractionPrompts();

            Debug.Log(
                glassToReturn.glassName +
                " returned to the counter successfully."
            );
        }
    }

    private void TryPickUpBeer(RaycastHit hit)
    {
        if (preparationMinigame == null ||
            !preparationMinigame.IsPreparationComplete())
        {
            Debug.Log(
                "Prepare the beer before picking one up."
            );
            return;
        }

        Beer beer =
            hit.collider.GetComponentInParent<Beer>();

        if (beer == null)
        {
            return;
        }

        if (!preparationMinigame.CanPickUpBeer(beer))
        {
            Debug.Log(
                "That beer is not assigned to you. " +
                "Find the assigned beer."
            );
            return;
        }

        PickUpBeer(beer);
        HideAllInteractionPrompts();
    }

    private void PickUpBeer(Beer beer)
    {
        if (beer == null)
        {
            return;
        }

        if (beerHoldPoint == null)
        {
            Debug.LogError(
                "FPController: Beer Hold Point is not assigned!"
            );
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

        Rigidbody beerRigidbody =
            beer.GetComponent<Rigidbody>();

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
        {
            return;
        }

        if (!TryGetLookedAtObject(out RaycastHit hit))
        {
            return;
        }

        HistoricalFigure npc =
            hit.collider.GetComponentInParent<HistoricalFigure>();

        if (npc == null || !npc.HasOrdered())
        {
            return;
        }

        Beer beerToServe = carriedBeer;

        npc.ServeBeer(beerToServe);

        beerToServe.gameObject.SetActive(false);
        carriedBeer = null;

        HideAllInteractionPrompts();

        Debug.Log(
            "Served " + beerToServe.beerName + " to Gandhi."
        );
    }

    private void ShowGlassPickupPrompt()
    {
        SetPromptVisible(
            glassPickupPrompt,
            glassPickupPanel,
            true
        );
    }

    private void ShowGlassDropOffPrompt()
    {
        SetPromptVisible(
            glassDropOffPrompt,
            glassDropOffPanel,
            true
        );
    }

    private void ShowBeerPickupPrompt()
    {
        SetPromptVisible(
            pickupPrompt,
            pickupPanel,
            true
        );
    }

    private void ShowHandInBeerPrompt()
    {
        SetPromptVisible(
            handInBeerPrompt,
            handInBeerPanel,
            true
        );
    }

    private void HideAllInteractionPrompts()
    {
        SetPromptVisible(
            glassPickupPrompt,
            glassPickupPanel,
            false
        );

        SetPromptVisible(
            glassDropOffPrompt,
            glassDropOffPanel,
            false
        );

        SetPromptVisible(
            pickupPrompt,
            pickupPanel,
            false
        );

        SetPromptVisible(
            handInBeerPrompt,
            handInBeerPanel,
            false
        );
    }

    private void SetPromptVisible(
        TextMeshProUGUI prompt,
        GameObject panel,
        bool visible)
    {
        if (prompt != null)
        {
            prompt.gameObject.SetActive(visible);
        }

        if (panel != null)
        {
            panel.SetActive(visible);
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

    public bool IsCarryingGlass()
    {
        return carriedGlass != null;
    }

    public CleanableGlass GetCarriedGlass()
    {
        return carriedGlass;
    }
}