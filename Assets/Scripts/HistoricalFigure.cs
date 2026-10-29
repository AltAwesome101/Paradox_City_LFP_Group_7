using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class HistoricalFigure : MonoBehaviour
{
    [Header("Entrance Settings")]
    public Transform entranceTarget;
    public Transform sittingPoint;

    [Tooltip("Initial delay before Gandhi can begin his entrance sequence.")]
    public float entranceDelay = 30f;

    [Tooltip("Extra time given to the player after all three glasses are cleaned.")]
    public float playerMoveDelayAfterCleaning = 5f;

    public float entranceSpeed = 2f;
    public float entranceTurnSpeed = 5f;
    public float arrivalDistance = 0.2f;
    public float entranceWalkTimeout = 30f;
    public float blockedMovementTimeout = 2f;

    [Header("Player Interaction")]
    public float interactionRange = 3f;
    public string playerLayerName = "Player";

    [Header("Order UI")]
    public TextMeshProUGUI orderText;
    public GameObject orderTextPanel;
    public float orderTextDuration = 10f;
    public float orderTextDistance = 3f;

    [Header("Meeting Result UI")]
    public TextMeshProUGUI meetingResultText;
    public GameObject meetingResultPanel;
    public float meetingResultDuration = 5f;

    [Header("Entrance Communication")]
    public TextMeshProUGUI communicationText;
    public GameObject communicationTextPanel;

    public string entranceMessage = "Gandhi is entering the tavern...";
    public string approachMessage = "Approach Gandhi to take his order.";
    public float approachMessageDuration = 5f;

    [Header("Beer Settings")]
    public string requestedBeer = "Correct Beer";

    [Header("Beer Pickup UI")]
    public TextMeshProUGUI beerPickupText;
    public GameObject beerPickupPanel;

    [Header("Beer Hand-In UI")]
    public TextMeshProUGUI beerHandInText;
    public GameObject beerHandInPanel;

    [Header("Meeting and Drinking")]
    public MeetingTimer meetingTimer;
    public NPCDrinking npcDrinking;

    [Header("Cinematic Camera")]
    public CinematicCameraFocus cinematicCamera;

    [Header("Level Completion")]
    public WrongBeerLevelCompletion wrongBeerCompletion;

    [Header("Leaving After Correct Beer")]
    public Transform exitDoorTarget;
    public float leaveSpeed = 2f;
    public float leaveDistance = 8f;

    [Header("Game Over")]
    public string gameOverSceneName = "GameOver";
    public float gameOverDelay = 3f;

    private Transform playerTransform;
    private CharacterController characterController;

    private bool canTakeOrder;
    private bool hasOrdered;
    private bool hasServedBeer;
    private bool receivedWrongBeer;
    private bool leaving;
    private bool entranceStarted;
    private bool entranceFinished;
    private bool endingSequenceStarted;

    private Vector3 leaveDirection;

    private Coroutine entranceCoroutine;
    private Coroutine orderUICoroutine;
    private Coroutine meetingResultCoroutine;
    private Coroutine communicationCoroutine;

    private Renderer[] npcRenderers;
    private Collider[] npcColliders;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        npcRenderers = GetComponentsInChildren<Renderer>(true);
        npcColliders = GetComponentsInChildren<Collider>(true);

        SetOrderUIVisible(false);
        SetMeetingResultUIVisible(false);
        SetCommunicationUIVisible(false);

        SetOptionalUIVisible(
            beerPickupText,
            beerPickupPanel,
            false
        );

        SetOptionalUIVisible(
            beerHandInText,
            beerHandInPanel,
            false
        );
    }

    private void Start()
    {
        if (entranceTarget == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Entrance Target has not been assigned."
            );

            return;
        }

        if (sittingPoint == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Sitting Point has not been assigned."
            );

            return;
        }

        if (characterController == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Gandhi needs a CharacterController."
            );

            return;
        }

        // Place Gandhi at the entrance while hidden.
        transform.SetPositionAndRotation(
            entranceTarget.position,
            entranceTarget.rotation
        );

        SetNPCVisible(false);

        canTakeOrder = false;

        entranceCoroutine =
            StartCoroutine(EnterTavernSequence());
    }

    private IEnumerator EnterTavernSequence()
    {
        // Step 1: Wait for the initial entrance delay.
        if (entranceDelay > 0f)
        {
            yield return new WaitForSeconds(entranceDelay);
        }

        // Step 2: Wait until all three glasses are returned.
        while (
            GlassCleaningProgress.Instance == null ||
            !GlassCleaningProgress.Instance.AllGlassesReturned
        )
        {
            yield return null;
        }

        // Step 3: Give the player five additional seconds.
        if (playerMoveDelayAfterCleaning > 0f)
        {
            yield return new WaitForSeconds(
                playerMoveDelayAfterCleaning
            );
        }

        // Step 4: Start Gandhi's entrance cinematic.
        entranceStarted = true;

        ShowCommunicationMessage(entranceMessage);
        SetNPCVisible(true);

        if (cinematicCamera != null)
        {
            cinematicCamera.FocusOnNPC(transform, true);
        }

        // Step 5: Walk toward the designated sitting point.
        yield return StartCoroutine(
            WalkToPoint(sittingPoint)
        );

        // Step 6: Return the camera to the player.
        if (cinematicCamera != null)
        {
            cinematicCamera.ReturnToPlayer();
        }

        HideCommunicationMessage();

        entranceFinished = true;
        canTakeOrder = true;

        // Step 7: Tell the player to approach Gandhi.
        ShowCommunicationMessage(approachMessage);

        if (communicationCoroutine != null)
        {
            StopCoroutine(communicationCoroutine);
        }

        communicationCoroutine = StartCoroutine(
            HideCommunicationAfterDelay(
                approachMessageDuration
            )
        );

        TryPlaceOrderForNearbyPlayer();

        Debug.Log(
            "[HistoricalFigure] Gandhi has reached his seat. " +
            "The player can now take his order."
        );
    }

    private IEnumerator WalkToPoint(Transform destination)
    {
        if (destination == null ||
            characterController == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float blockedTime = 0f;

        Vector3 previousPosition = transform.position;

        while (
            Vector3.Distance(
                transform.position,
                destination.position
            ) > arrivalDistance &&
            elapsed < entranceWalkTimeout
        )
        {
            Vector3 direction =
                destination.position - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();

                Quaternion targetRotation =
                    Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    entranceTurnSpeed * Time.deltaTime
                );

                // CharacterController.Move respects solid colliders.
                characterController.Move(
                    direction * entranceSpeed * Time.deltaTime
                );
            }

            float distanceMoved = Vector3.Distance(
                transform.position,
                previousPosition
            );

            if (distanceMoved < 0.005f)
            {
                blockedTime += Time.deltaTime;
            }
            else
            {
                blockedTime = 0f;
            }

            previousPosition = transform.position;

            if (blockedTime >= blockedMovementTimeout)
            {
                Debug.LogWarning(
                    "[HistoricalFigure] Gandhi's movement is blocked. " +
                    "Check his path, sitting point and colliders."
                );

                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (
            Vector3.Distance(
                transform.position,
                destination.position
            ) > arrivalDistance
        )
        {
            Debug.LogWarning(
                "[HistoricalFigure] Gandhi did not reach the exact " +
                "sitting point. Order interaction will still be unlocked."
            );
        }
    }

    private void SetNPCVisible(bool visible)
    {
        if (npcRenderers != null)
        {
            foreach (Renderer npcRenderer in npcRenderers)
            {
                if (npcRenderer != null)
                {
                    npcRenderer.enabled = visible;
                }
            }
        }

        if (npcColliders != null)
        {
            foreach (Collider npcCollider in npcColliders)
            {
                if (npcCollider != null)
                {
                    npcCollider.enabled = visible;
                }
            }
        }

        if (characterController != null)
        {
            characterController.enabled = visible;
        }
    }

    private void Update()
    {
        if (leaving)
        {
            MoveTowardsExit();
            return;
        }

        if (playerTransform != null)
        {
            float distance = Vector3.Distance(
                playerTransform.position,
                transform.position
            );

            if (distance > orderTextDistance)
            {
                SetOrderUIVisible(false);
            }
            else
            {
                TryPlaceOrderForNearbyPlayer();
            }
        }

        if (!hasServedBeer || endingSequenceStarted)
        {
            return;
        }

        if (
            meetingTimer != null &&
            meetingTimer.IsMeetingMissed()
        )
        {
            HandleMissedMeeting();
        }

        if (
            npcDrinking != null &&
            npcDrinking.HasFinishedDrinking() &&
            !endingSequenceStarted &&
            meetingTimer != null &&
            !meetingTimer.IsMeetingMissed()
        )
        {
            if (!receivedWrongBeer)
            {
                StartCoroutine(CorrectBeerEnding());
            }
        }
    }

    private void TryPlaceOrderForNearbyPlayer()
    {
        if (!canTakeOrder ||
            hasOrdered ||
            playerTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(
            playerTransform.position,
            transform.position
        );

        if (distance <= orderTextDistance)
        {
            PlaceOrder();
        }
    }

    private void PlaceOrder()
    {
        if (!canTakeOrder || hasOrdered)
        {
            return;
        }

        hasOrdered = true;

        ShowOrderMessage(
            "Mahatma Gandhi: I'll have a beer, please."
        );
    }

    // PUBLIC METHOD: Used by FPController to check the order status.
    public bool HasOrdered()
    {
        return hasOrdered;
    }

    private void ShowOrderMessage(string message)
    {
        if (orderText != null)
        {
            orderText.text = message;
        }

        SetOrderUIVisible(true);

        if (orderUICoroutine != null)
        {
            StopCoroutine(orderUICoroutine);
        }

        orderUICoroutine = StartCoroutine(
            HideOrderAfterDelay(orderTextDuration)
        );
    }

    private IEnumerator HideOrderAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        SetOrderUIVisible(false);
        orderUICoroutine = null;
    }

    // Receives the beer name from FPController.
    public void ServeBeer(string beerName)
    {
        if (!hasOrdered || hasServedBeer)
        {
            return;
        }

        hasServedBeer = true;

        receivedWrongBeer = !string.Equals(
            beerName,
            requestedBeer,
            System.StringComparison.OrdinalIgnoreCase
        );

        ShowOrderMessage("Mahatma Gandhi: Thanks.");

        // Start the meeting timer when Gandhi receives the beer.
        if (meetingTimer != null)
        {
            meetingTimer.StartMeetingTimer();
        }

        // Start drinking with the correct/wrong beer outcome.
        if (npcDrinking != null)
        {
            npcDrinking.StartDrinking(receivedWrongBeer);
        }

        // Preserve the beer-serving cinematic.
        if (cinematicCamera != null)
        {
            cinematicCamera.FocusOnNPC(transform);
        }

        Debug.Log(
            receivedWrongBeer
                ? "[HistoricalFigure] Gandhi received the wrong beer."
                : "[HistoricalFigure] Gandhi received the correct beer."
        );
    }

    private IEnumerator CorrectBeerEnding()
    {
        if (endingSequenceStarted)
        {
            yield break;
        }

        endingSequenceStarted = true;

        ShowMeetingResultMessage("HEADING TO MEETING");

        yield return new WaitForSeconds(1f);

        if (
            cinematicCamera != null &&
            cinematicCamera.IsFocusingOnNPC()
        )
        {
            cinematicCamera.ReturnToPlayer();
        }

        if (exitDoorTarget != null)
        {
            leaveDirection =
                exitDoorTarget.position - transform.position;

            leaveDirection.y = 0f;

            if (leaveDirection.sqrMagnitude > 0.001f)
            {
                leaveDirection.Normalize();
            }
            else
            {
                leaveDirection = transform.forward;
            }
        }
        else
        {
            leaveDirection = transform.forward;
        }

        leaving = true;

        yield return new WaitForSeconds(gameOverDelay);

        if (!Application.CanStreamedLevelBeLoaded(gameOverSceneName))
        {
            Debug.LogError(
                "[HistoricalFigure] Game Over scene '" +
                gameOverSceneName +
                "' cannot be loaded. Check the scene name and Build Settings."
            );

            leaving = false;
            yield break;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameOverSceneName);
    }

    private void MoveTowardsExit()
    {
        if (characterController == null ||
            !characterController.enabled)
        {
            return;
        }

        if (exitDoorTarget != null)
        {
            Vector3 direction =
                exitDoorTarget.position - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                direction.Normalize();

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    entranceTurnSpeed * Time.deltaTime
                );

                characterController.Move(
                    direction * leaveSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            characterController.Move(
                leaveDirection * leaveSpeed * Time.deltaTime
            );
        }
    }

    private void HandleMissedMeeting()
    {
        if (endingSequenceStarted)
        {
            return;
        }

        // Wait until Gandhi finishes drinking before deciding the outcome.
        if (
            npcDrinking != null &&
            !npcDrinking.HasFinishedDrinking()
        )
        {
            return;
        }

        endingSequenceStarted = true;

        ShowMeetingResultMessage("MEETING MISSED");

        if (cinematicCamera != null)
        {
            cinematicCamera.ReturnToPlayer();
        }

        if (receivedWrongBeer)
        {
            StartCoroutine(CompleteWrongBeerEnding());
        }
    }

    private IEnumerator CompleteWrongBeerEnding()
    {
        yield return new WaitForSeconds(meetingResultDuration);

        if (wrongBeerCompletion != null)
        {
            wrongBeerCompletion.WrongBeerGiven();
        }
        else
        {
            Debug.LogError(
                "[HistoricalFigure] WrongBeerLevelCompletion " +
                "has not been assigned."
            );
        }
    }

    private void ShowMeetingResultMessage(string message)
    {
        if (meetingResultText != null)
        {
            meetingResultText.text = message;
        }

        SetMeetingResultUIVisible(true);

        if (meetingResultCoroutine != null)
        {
            StopCoroutine(meetingResultCoroutine);
        }

        meetingResultCoroutine = StartCoroutine(
            HideMeetingResultAfterDelay(meetingResultDuration)
        );
    }

    private IEnumerator HideMeetingResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        SetMeetingResultUIVisible(false);
        meetingResultCoroutine = null;
    }

    private void ShowCommunicationMessage(string message)
    {
        if (communicationText != null)
        {
            communicationText.text = message;
        }

        SetCommunicationUIVisible(true);
    }

    private IEnumerator HideCommunicationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        HideCommunicationMessage();
        communicationCoroutine = null;
    }

    private void HideCommunicationMessage()
    {
        if (communicationCoroutine != null)
        {
            StopCoroutine(communicationCoroutine);
            communicationCoroutine = null;
        }

        SetCommunicationUIVisible(false);
    }

    private void SetOrderUIVisible(bool visible)
    {
        if (orderText != null)
        {
            orderText.gameObject.SetActive(visible);
        }

        if (orderTextPanel != null)
        {
            orderTextPanel.SetActive(visible);
        }
    }

    private void SetMeetingResultUIVisible(bool visible)
    {
        if (meetingResultText != null)
        {
            meetingResultText.gameObject.SetActive(visible);
        }

        if (meetingResultPanel != null)
        {
            meetingResultPanel.SetActive(visible);
        }
    }

    private void SetCommunicationUIVisible(bool visible)
    {
        if (communicationText != null)
        {
            communicationText.gameObject.SetActive(visible);
        }

        if (communicationTextPanel != null)
        {
            communicationTextPanel.SetActive(visible);
        }
    }

    private void SetOptionalUIVisible(
        TextMeshProUGUI textElement,
        GameObject panel,
        bool visible
    )
    {
        if (textElement != null)
        {
            textElement.gameObject.SetActive(visible);
        }

        if (panel != null)
        {
            panel.SetActive(visible);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (
            other.gameObject.layer !=
            LayerMask.NameToLayer(playerLayerName)
        )
        {
            return;
        }

        playerTransform = other.transform;

        TryPlaceOrderForNearbyPlayer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == playerTransform)
        {
            playerTransform = null;
            SetOrderUIVisible(false);
        }
    }

    private void OnDisable()
    {
        if (communicationCoroutine != null)
        {
            StopCoroutine(communicationCoroutine);
            communicationCoroutine = null;
        }

        if (cinematicCamera != null &&
            cinematicCamera.IsFocusingOnNPC())
        {
            cinematicCamera.ReturnToPlayer();
        }
    }
}