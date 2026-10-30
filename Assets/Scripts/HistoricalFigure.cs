using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class HistoricalFigure : MonoBehaviour
{
    [Header("Order")]
    public string requestedBeer = "Correct Beer";

    [Header("Delayed Tavern Entrance")]
    public Transform entranceTarget;
    public Transform sittingPoint;
    public float entranceDelay = 5f;
    public float entranceSpeed = 2f;
    public float entranceTurnSpeed = 180f;
    public float sittingArrivalDistance = 0.2f;
    public float entranceWalkTimeout = 20f;
    public float blockedMovementTimeout = 1.5f;

    [Header("Entrance Communication")]
    public TextMeshProUGUI communicationText;
    public GameObject communicationTextPanel;

    public string entranceMessage = "Gandhi is entering the tavern...";
    public string approachMessage = "Approach Gandhi to take his order.";
    public float approachMessageDuration = 5f;

    [Header("Meeting")]
    public MeetingTimer meetingTimer;

    [Header("Drinking")]
    public NPCDrinking npcDrinking;

    [Header("Order UI")]
    public TextMeshProUGUI orderText;
    public GameObject orderTextPanel;
    public float orderTextDuration = 10f;
    public float orderTextDistance = 3f;

    [Header("Meeting Result UI")]
    public TextMeshProUGUI meetingResultText;
    public GameObject meetingResultPanel;

    [Header("Cinematic Camera")]
    public CinematicCameraFocus cinematicCamera;

    [Header("Wrong Beer Completion")]
    public WrongBeerLevelCompletion wrongBeerCompletion;

    [Header("NPC Door Movement")]
    public Transform doorTarget;
    public float leaveSpeed = 2f;
    public float turnSpeed = 180f;
    public float doorArrivalDistance = 0.2f;
    public float modelRotationOffset = 0f;

    [Header("Game Over")]
    public string gameOverSceneName = "GameOver";
    public float gameOverDelay = 3f;

    private bool hasOrdered;
    private bool hasBeenServed;
    private bool receivedWrongBeer;
    private bool meetingMissed;
    private bool meetingAttended;
    private bool resultShown;
    private bool endingTriggered;
    private bool canTakeOrder;
    private bool entranceFinished;

    private Transform playerTransform;
    private CharacterController characterController;

    private Coroutine orderTextCoroutine;
    private Coroutine communicationCoroutine;
    private Coroutine leaveCoroutine;
    private Coroutine entranceCoroutine;

    private Renderer[] characterRenderers;
    private bool[] originalRendererStates;

    private Collider[] characterColliders;
    private bool[] originalColliderStates;

    public bool HasOrdered() => hasOrdered;
    public string GetRequestedBeer() => requestedBeer;
    public bool HasBeenServed() => hasBeenServed;
    public bool ReceivedWrongBeer() => receivedWrongBeer;
    public bool MeetingWasMissed() => meetingMissed;
    public bool MeetingWasAttended() => meetingAttended;

    public bool CanTakeOrder()
    {
        return canTakeOrder &&
               !hasOrdered &&
               !hasBeenServed &&
               !endingTriggered;
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        SetOrderUIVisible(false);
        SetMeetingResultUIVisible(false);
        SetCommunicationUIVisible(false);

        // If entrance markers are not assigned, Gandhi stays
        // at his existing scene position and can take an order.
        if (entranceTarget == null || sittingPoint == null)
        {
            Debug.LogWarning(
                "[HistoricalFigure] Entrance Target or Sitting Point " +
                "is missing. Gandhi will remain at his current position."
            );

            canTakeOrder = true;
            entranceFinished = true;
            ShowApproachMessage();
            return;
        }

        if (characterController == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Add a CharacterController to Gandhi."
            );

            canTakeOrder = true;
            entranceFinished = true;
            ShowApproachMessage();
            return;
        }

        characterRenderers = GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[characterRenderers.Length];

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            originalRendererStates[i] = characterRenderers[i].enabled;
        }

        characterColliders = GetComponentsInChildren<Collider>(true);
        originalColliderStates = new bool[characterColliders.Length];

        for (int i = 0; i < characterColliders.Length; i++)
        {
            originalColliderStates[i] = characterColliders[i].enabled;
        }

        transform.position = entranceTarget.position;
        transform.rotation = entranceTarget.rotation;

        SetCharacterVisible(false);
        SetCharacterCollidersEnabled(false);

        canTakeOrder = false;
        entranceFinished = false;

        entranceCoroutine = StartCoroutine(EnterTavernSequence());
    }

    private IEnumerator EnterTavernSequence()
    {
        // FIRST: Wait until all three glasses have been cleaned
        // and returned to their correct places.
        Debug.Log(
            "[HistoricalFigure] Waiting for the player to return all " +
            "three glasses before Gandhi can enter."
        );

        while (GlassCleaningProgress.Instance == null ||
               !GlassCleaningProgress.Instance.AllGlassesReturned)
        {
            yield return null;
        }

        Debug.Log(
            "[HistoricalFigure] All three glasses have been returned. " +
            "Gandhi's entrance delay can now begin."
        );

        // SECOND: Start the entrance delay only after all glasses
        // have been returned.
        if (entranceDelay > 0f)
        {
            yield return new WaitForSeconds(entranceDelay);
        }

        if (entranceTarget == null ||
            sittingPoint == null ||
            characterController == null)
        {
            canTakeOrder = true;
            entranceFinished = true;
            entranceCoroutine = null;
            ShowApproachMessage();
            yield break;
        }

        Debug.Log("[HistoricalFigure] Gandhi is entering the tavern.");

        // Display the entrance communication.
        ShowCommunication(entranceMessage);

        SetCharacterVisible(true);
        SetCharacterCollidersEnabled(true);

        // CINEMATIC: focus on Gandhi and follow him during entrance.
        if (cinematicCamera != null)
        {
            cinematicCamera.FocusOnEntrance(transform);
        }

        float totalWalkTime = 0f;
        float blockedTime = 0f;
        bool reachedSittingPoint = false;

        while (totalWalkTime < entranceWalkTimeout)
        {
            Vector3 targetPosition = sittingPoint.position;
            targetPosition.y = transform.position.y;

            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.magnitude <= sittingArrivalDistance)
            {
                reachedSittingPoint = true;
                break;
            }

            direction.Normalize();

            Quaternion targetRotation =
                Quaternion.LookRotation(direction, Vector3.up) *
                Quaternion.Euler(0f, modelRotationOffset, 0f);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                entranceTurnSpeed * Time.deltaTime
            );

            Vector3 positionBeforeMove = transform.position;

            characterController.Move(
                direction * entranceSpeed * Time.deltaTime
            );

            float movedDistance = Vector3.Distance(
                positionBeforeMove,
                transform.position
            );

            if (movedDistance < 0.005f)
            {
                blockedTime += Time.deltaTime;
            }
            else
            {
                blockedTime = 0f;
            }

            totalWalkTime += Time.deltaTime;

            if (blockedTime >= blockedMovementTimeout)
            {
                Debug.LogWarning(
                    "[HistoricalFigure] Gandhi is blocked on the way " +
                    "to his seat. He will stop here, but order interaction " +
                    "will still become available."
                );
                break;
            }

            yield return null;
        }

        Vector3 currentFlat = new Vector3(
            transform.position.x, 0f, transform.position.z
        );

        Vector3 seatFlat = new Vector3(
            sittingPoint.position.x, 0f, sittingPoint.position.z
        );

        float finalDistance = Vector3.Distance(currentFlat, seatFlat);

        if (finalDistance <= Mathf.Max(sittingArrivalDistance, 0.35f))
        {
            reachedSittingPoint = true;
        }

        if (reachedSittingPoint)
        {
            transform.rotation = sittingPoint.rotation *
                Quaternion.Euler(0f, modelRotationOffset, 0f);
        }
        else
        {
            Debug.LogWarning(
                "[HistoricalFigure] Gandhi did not reach the exact seat. " +
                "He will remain at his current position."
            );
        }

        // End the entrance cinematic and return control to the player.
        if (cinematicCamera != null)
        {
            cinematicCamera.ReturnToPlayer();
        }

        // End the entrance sequence and make Gandhi available.
        canTakeOrder = true;
        entranceFinished = true;
        entranceCoroutine = null;

        Debug.Log(
            "[HistoricalFigure] Gandhi's entrance has ended. " +
            "He can now take the player's order."
        );

        ShowApproachMessage();
        TryPlaceOrderForNearbyPlayer();
    }

    // -------------------------------
    // ENTRANCE COMMUNICATION
    // -------------------------------

    private void ShowCommunication(string message)
    {
        if (communicationText == null)
        {
            Debug.LogWarning(
                "[HistoricalFigure] Communication Text is not assigned."
            );
            return;
        }

        if (communicationCoroutine != null)
        {
            StopCoroutine(communicationCoroutine);
            communicationCoroutine = null;
        }

        communicationText.text = message;
        SetCommunicationUIVisible(true);
    }

    private void ShowApproachMessage()
    {
        if (string.IsNullOrEmpty(approachMessage))
        {
            SetCommunicationUIVisible(false);
            return;
        }

        ShowCommunication(approachMessage);

        if (communicationCoroutine != null)
        {
            StopCoroutine(communicationCoroutine);
        }

        communicationCoroutine =
            StartCoroutine(HideCommunicationAfterDelay(
                approachMessageDuration
            ));
    }

    private IEnumerator HideCommunicationAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        SetCommunicationUIVisible(false);
        communicationCoroutine = null;
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

    private void SetCharacterVisible(bool visible)
    {
        if (characterRenderers == null)
        {
            return;
        }

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null)
            {
                characterRenderers[i].enabled = visible
                    ? originalRendererStates[i]
                    : false;
            }
        }
    }

    private void SetCharacterCollidersEnabled(bool enabledState)
    {
        if (characterColliders == null)
        {
            return;
        }

        for (int i = 0; i < characterColliders.Length; i++)
        {
            if (characterColliders[i] != null)
            {
                characterColliders[i].enabled = enabledState
                    ? originalColliderStates[i]
                    : false;
            }
        }
    }

    public void PlaceOrder()
    {
        if (!CanTakeOrder())
        {
            return;
        }

        hasOrdered = true;

        Debug.Log(
            "[HistoricalFigure] Gandhi ordered: " + requestedBeer
        );

        ShowOrderMessage("Mahatma Gandhi: I'll have a beer, please.");
        SetMeetingResultUIVisible(false);
    }

    private void ShowOrderMessage(string message)
    {
        if (orderText == null)
        {
            Debug.LogWarning(
                "[HistoricalFigure] Order Text is not assigned."
            );
            return;
        }

        if (orderTextCoroutine != null)
        {
            StopCoroutine(orderTextCoroutine);
            orderTextCoroutine = null;
        }

        orderText.text = message;
        SetOrderUIVisible(true);

        orderTextCoroutine =
            StartCoroutine(HideOrderTextAfterDelay());
    }

    private IEnumerator HideOrderTextAfterDelay()
    {
        yield return new WaitForSecondsRealtime(orderTextDuration);
        HideOrderText();
    }

    private void HideOrderText()
    {
        SetOrderUIVisible(false);

        if (orderTextCoroutine != null)
        {
            StopCoroutine(orderTextCoroutine);
            orderTextCoroutine = null;
        }
    }

    private void Update()
    {
        if (orderText != null &&
            orderText.gameObject.activeSelf &&
            playerTransform != null)
        {
            float distance = Vector3.Distance(
                playerTransform.position,
                transform.position
            );

            if (distance > orderTextDistance)
            {
                HideOrderText();
            }
        }

        TryPlaceOrderForNearbyPlayer();

        if (endingTriggered)
        {
            return;
        }

        // The wrong-beer route ends when the accelerated timer expires.
        if (meetingTimer != null &&
            meetingTimer.IsMeetingMissed() &&
            !meetingMissed)
        {
            HandleMeetingMissed();
            return;
        }

        // Only the correct-beer route can finish drinking and leave
        // for the meeting.
        if (!receivedWrongBeer &&
            hasBeenServed &&
            npcDrinking != null &&
            npcDrinking.HasFinishedDrinking() &&
            !meetingAttended &&
            !meetingMissed)
        {
            bool stillInTime =
                meetingTimer == null ||
                (!meetingTimer.IsMeetingMissed() &&
                 meetingTimer.GetRemainingTime() > 0f);

            if (stillInTime)
            {
                AttendMeeting();
            }
            else if (meetingTimer != null &&
                     meetingTimer.IsMeetingMissed())
            {
                HandleMeetingMissed();
            }
        }
    }

    private void TryPlaceOrderForNearbyPlayer()
    {
        if (!canTakeOrder ||
            hasOrdered ||
            hasBeenServed ||
            endingTriggered ||
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

    public void ServeBeer(Beer beer)
    {
        if (!hasOrdered || hasBeenServed || beer == null)
        {
            return;
        }

        hasBeenServed = true;

        // Gandhi requested the correct beer but does not recognise
        // whether the player has handed him the wrong beer.
        receivedWrongBeer = beer.IsWrongBeer();

        ShowOrderMessage("Mahatma Gandhi: Thanks.");

        if (meetingTimer != null)
        {
            // Requires the compatible overload in MeetingTimer.cs.
            meetingTimer.StartMeetingTimer(receivedWrongBeer);
        }

        if (npcDrinking != null)
        {
            npcDrinking.StartDrinking(receivedWrongBeer);
        }
        else
        {
            Debug.LogError(
                "[HistoricalFigure] NPCDrinking is not assigned."
            );
        }

        // CINEMATIC: focus on Gandhi and follow him during drinking.
        if (cinematicCamera != null)
        {
            cinematicCamera.FocusOnDrinking(transform);
        }

        if (receivedWrongBeer)
        {
            Debug.Log(
                "[HistoricalFigure] WRONG BEER SERVED. " +
                "Gandhi will become drunk and miss the meeting."
            );
        }
        else
        {
            Debug.Log(
                "[HistoricalFigure] CORRECT BEER SERVED. " +
                "Gandhi will finish drinking quickly and leave for the meeting."
            );
        }
    }

    private void AttendMeeting()
    {
        if (meetingAttended || endingTriggered)
        {
            return;
        }

        meetingAttended = true;
        endingTriggered = true;

        HideOrderText();

        if (meetingTimer != null)
        {
            meetingTimer.StopMeetingTimer();
        }

        if (npcDrinking != null)
        {
            npcDrinking.StopDrinking();
        }

        Debug.Log(
            "[HistoricalFigure] Gandhi finished the correct beer in time. " +
            "He is leaving for the meeting. Game Over will follow."
        );

        if (meetingResultText != null)
        {
            meetingResultText.text = "HEADING TO MEETING";
            SetMeetingResultUIVisible(true);
        }

        if (cinematicCamera != null)
        {
            cinematicCamera.ReturnToPlayer();
        }

        if (doorTarget == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Door Target is not assigned. " +
                "Assign an empty GameObject at the tavern exit."
            );

            leaveCoroutine = StartCoroutine(LoadGameOverAfterDelay());
            return;
        }

        leaveCoroutine = StartCoroutine(WalkToDoor());
    }

    private IEnumerator WalkToDoor()
    {
        if (characterController == null)
        {
            Debug.LogWarning(
                "[HistoricalFigure] CharacterController is missing. " +
                "Loading Game Over without the exit movement."
            );

            yield return StartCoroutine(LoadGameOverAfterDelay());
            yield break;
        }

        float blockedTime = 0f;

        while (doorTarget != null)
        {
            Vector3 targetPosition = doorTarget.position;
            targetPosition.y = transform.position.y;

            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.magnitude <= doorArrivalDistance)
            {
                break;
            }

            direction.Normalize();

            Quaternion targetRotation =
                Quaternion.LookRotation(direction, Vector3.up) *
                Quaternion.Euler(0f, modelRotationOffset, 0f);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            Vector3 beforeMove = transform.position;

            characterController.Move(
                direction * leaveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(beforeMove, transform.position) < 0.005f)
            {
                blockedTime += Time.deltaTime;
            }
            else
            {
                blockedTime = 0f;
            }

            if (blockedTime >= 3f)
            {
                Debug.LogWarning(
                    "[HistoricalFigure] Gandhi is blocked on the way " +
                    "to the door. Continuing to Game Over."
                );
                break;
            }

            yield return null;
        }

        Debug.Log("[HistoricalFigure] Gandhi's exit sequence has ended.");

        yield return StartCoroutine(LoadGameOverAfterDelay());
    }

    private IEnumerator LoadGameOverAfterDelay()
    {
        if (gameOverDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(gameOverDelay);
        }

        if (!Application.CanStreamedLevelBeLoaded(gameOverSceneName))
        {
            Debug.LogError(
                "[HistoricalFigure] Game Over scene '" +
                gameOverSceneName +
                "' cannot be loaded. Check the exact scene name and " +
                "make sure it is included in Build Settings."
            );
            yield break;
        }

        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("[HistoricalFigure] Loading Game Over scene.");
        SceneManager.LoadScene(gameOverSceneName);
    }

    private void HandleMeetingMissed()
    {
        if (meetingMissed || endingTriggered)
        {
            return;
        }

        meetingMissed = true;

        HideOrderText();

        if (meetingTimer != null)
        {
            meetingTimer.StopMeetingTimer();
        }

        Debug.Log("[HistoricalFigure] MEETING MISSED.");

        if (cinematicCamera != null)
        {
            cinematicCamera.ReturnToPlayer();
        }

        ShowMeetingMissed();

        if (receivedWrongBeer)
        {
            endingTriggered = true;

            if (npcDrinking != null)
            {
                npcDrinking.StopDrinking();
            }

            Debug.Log(
                "[HistoricalFigure] Wrong beer caused the missed meeting. " +
                "Triggering Game Win."
            );

            if (wrongBeerCompletion != null)
            {
                wrongBeerCompletion.WrongBeerGiven();
            }
            else
            {
                Debug.LogError(
                    "[HistoricalFigure] WrongBeerLevelCompletion is not " +
                    "assigned. Assign it in the Inspector."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[HistoricalFigure] Gandhi missed the meeting on the " +
                "correct-beer route. Game Win will not be triggered."
            );
        }
    }

    private void ShowMeetingMissed()
    {
        if (resultShown)
        {
            return;
        }

        resultShown = true;

        if (meetingResultText != null)
        {
            meetingResultText.text = "MEETING MISSED";
            SetMeetingResultUIVisible(true);
            StartCoroutine(HideMeetingResult());
        }
    }

    private IEnumerator HideMeetingResult()
    {
        yield return new WaitForSecondsRealtime(5f);
        SetMeetingResultUIVisible(false);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerTransform = other.transform;
            PlaceOrder();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player") &&
            playerTransform == other.transform)
        {
            HideOrderText();
            playerTransform = null;
        }
    }

    private void OnDisable()
    {
        if (orderTextCoroutine != null)
        {
            StopCoroutine(orderTextCoroutine);
            orderTextCoroutine = null;
        }

        if (communicationCoroutine != null)
        {
            StopCoroutine(communicationCoroutine);
            communicationCoroutine = null;
        }

        if (leaveCoroutine != null)
        {
            StopCoroutine(leaveCoroutine);
            leaveCoroutine = null;
        }

        if (entranceCoroutine != null)
        {
            StopCoroutine(entranceCoroutine);
            entranceCoroutine = null;
        }

        // Avoid leaving player controls disabled if Gandhi is disabled.
        if (cinematicCamera != null &&
            cinematicCamera.IsFocusingOnNPC())
        {
            cinematicCamera.ReturnToPlayer();
        }
    }
}