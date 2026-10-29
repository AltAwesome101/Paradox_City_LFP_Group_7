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
    public float entranceDelay = 30f;
    public float entranceSpeed = 2f;
    public float entranceTurnSpeed = 180f;
    public float sittingArrivalDistance = 0.2f;

    [Tooltip("Maximum time Gandhi tries to reach his seat.")]
    public float entranceWalkTimeout = 20f;

    [Tooltip("Time without movement before Gandhi stops trying.")]
    public float blockedMovementTimeout = 1.5f;

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

    private Transform playerTransform;
    private CharacterController characterController;

    private Coroutine orderTextCoroutine;
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
        return canTakeOrder && !hasOrdered &&
               !hasBeenServed && !endingTriggered;
    }

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        SetOrderUIVisible(false);
        SetMeetingResultUIVisible(false);

        if (entranceTarget == null || sittingPoint == null)
        {
            Debug.LogWarning(
                "[HistoricalFigure] Entrance Target or Sitting Point " +
                "is missing. Gandhi will remain at his current position."
            );

            canTakeOrder = true;
            return;
        }

        if (characterController == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Add a CharacterController to Gandhi."
            );

            canTakeOrder = true;
            return;
        }

        characterRenderers = GetComponentsInChildren<Renderer>(true);
        originalRendererStates = new bool[characterRenderers.Length];

        for (int i = 0; i < characterRenderers.Length; i++)
            originalRendererStates[i] = characterRenderers[i].enabled;

        characterColliders = GetComponentsInChildren<Collider>(true);
        originalColliderStates = new bool[characterColliders.Length];

        for (int i = 0; i < characterColliders.Length; i++)
            originalColliderStates[i] = characterColliders[i].enabled;

        // Move Gandhi to his starting point before the delay.
        transform.position = entranceTarget.position;
        transform.rotation = entranceTarget.rotation;

        SetCharacterVisible(false);
        SetCharacterCollidersEnabled(false);

        canTakeOrder = false;
        entranceCoroutine = StartCoroutine(EnterTavernSequence());
    }

    private IEnumerator EnterTavernSequence()
    {
        if (entranceDelay > 0f)
            yield return new WaitForSeconds(entranceDelay);

        if (entranceTarget == null || sittingPoint == null ||
            characterController == null)
        {
            canTakeOrder = true;
            entranceCoroutine = null;
            yield break;
        }

        Debug.Log("[HistoricalFigure] Gandhi is entering the tavern.");

        SetCharacterVisible(true);
        SetCharacterCollidersEnabled(true);

        float totalWalkTime = 0f;
        float blockedTime = 0f;
        bool reachedSittingPoint = false;

        // Walk toward the seat using collision-aware movement.
        while (totalWalkTime < entranceWalkTimeout)
        {
            Vector3 targetPosition = sittingPoint.position;
            targetPosition.y = transform.position.y;

            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance <= sittingArrivalDistance)
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
                blockedTime += Time.deltaTime;
            else
                blockedTime = 0f;

            totalWalkTime += Time.deltaTime;

            if (blockedTime >= blockedMovementTimeout)
            {
                Debug.LogWarning(
                    "[HistoricalFigure] Gandhi is blocked on the way " +
                    "to his seat. He will stop here and order interaction " +
                    "will be enabled. Adjust Sitting Point or add waypoints."
                );
                break;
            }

            yield return null;
        }

        // Check whether Gandhi reached the seat closely enough.
        Vector3 currentFlat = new Vector3(
            transform.position.x, 0f, transform.position.z
        );

        Vector3 seatFlat = new Vector3(
            sittingPoint.position.x, 0f, sittingPoint.position.z
        );

        float finalDistance = Vector3.Distance(currentFlat, seatFlat);

        if (finalDistance <= Mathf.Max(sittingArrivalDistance, 0.35f))
            reachedSittingPoint = true;

        // Never force Gandhi through the counter to reach the marker.
        // This script does not play a real sitting animation.
        if (reachedSittingPoint)
        {
            transform.rotation = sittingPoint.rotation *
                Quaternion.Euler(0f, modelRotationOffset, 0f);
        }
        else
        {
            Debug.LogWarning(
                "[HistoricalFigure] Gandhi did not reach the exact seat. " +
                "He will remain at his current position, but the player " +
                "can still take his order."
            );
        }

        // CRITICAL FIX: always unlock the order after the entrance attempt.
        canTakeOrder = true;
        entranceCoroutine = null;

        Debug.Log(
            "[HistoricalFigure] Gandhi's entrance sequence has ended. " +
            "He is now available for the player's order."
        );

        TryPlaceOrderForNearbyPlayer();
    }

    private void SetCharacterVisible(bool visible)
    {
        if (characterRenderers == null)
            return;

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            if (characterRenderers[i] != null)
                characterRenderers[i].enabled =
                    visible ? originalRendererStates[i] : false;
        }
    }

    private void SetCharacterCollidersEnabled(bool enabled)
    {
        if (characterColliders == null)
            return;

        for (int i = 0; i < characterColliders.Length; i++)
        {
            if (characterColliders[i] != null)
                characterColliders[i].enabled =
                    enabled ? originalColliderStates[i] : false;
        }
    }

    public void PlaceOrder()
    {
        if (!CanTakeOrder())
            return;

        hasOrdered = true;

        Debug.Log("[HistoricalFigure] Gandhi ordered: " + requestedBeer);

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
        orderTextCoroutine = StartCoroutine(HideOrderTextAfterDelay());
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
                HideOrderText();
        }

        TryPlaceOrderForNearbyPlayer();

        if (endingTriggered)
            return;

        if (meetingTimer != null &&
            meetingTimer.IsMeetingMissed() &&
            !meetingMissed)
        {
            HandleMeetingMissed();
            return;
        }

        if (!receivedWrongBeer &&
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
                AttendMeeting();
        }
    }

    private void TryPlaceOrderForNearbyPlayer()
    {
        if (!canTakeOrder || hasOrdered || hasBeenServed ||
            endingTriggered || playerTransform == null)
            return;

        float distance = Vector3.Distance(
            playerTransform.position,
            transform.position
        );

        if (distance <= orderTextDistance)
            PlaceOrder();
    }

    public void ServeBeer(Beer beer)
    {
        if (!hasOrdered || hasBeenServed || beer == null)
            return;

        hasBeenServed = true;
        receivedWrongBeer = beer.IsWrongBeer();

        ShowOrderMessage("Mahatma Gandhi: Thanks.");

        if (meetingTimer != null)
            meetingTimer.StartMeetingTimer();

        if (npcDrinking != null)
            npcDrinking.StartDrinking(receivedWrongBeer);

        if (cinematicCamera != null)
            cinematicCamera.FocusOnNPC(transform);

        if (receivedWrongBeer)
        {
            Debug.Log("[HistoricalFigure] WRONG BEER SERVED.");
            Debug.Log("[HistoricalFigure] Gandhi is unaware of the mistake.");
        }
        else
        {
            Debug.Log("[HistoricalFigure] CORRECT BEER SERVED.");
        }
    }

    private void AttendMeeting()
    {
        if (meetingAttended || endingTriggered)
            return;

        meetingAttended = true;
        endingTriggered = true;

        HideOrderText();

        Debug.Log(
            "[HistoricalFigure] Correct beer finished in time. " +
            "Gandhi is leaving for the meeting."
        );

        if (meetingResultText != null)
        {
            meetingResultText.text = "HEADING TO MEETING";
            SetMeetingResultUIVisible(true);
        }

        if (cinematicCamera != null)
            cinematicCamera.ReturnToPlayer();

        if (doorTarget == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Door Target is not assigned."
            );

            StartCoroutine(LoadGameOverAfterDelay());
            return;
        }

        leaveCoroutine = StartCoroutine(WalkToDoor());
    }

    private IEnumerator WalkToDoor()
    {
        if (characterController == null)
        {
            Debug.LogError(
                "[HistoricalFigure] CharacterController is missing."
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
                break;

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
                blockedTime += Time.deltaTime;
            else
                blockedTime = 0f;

            // Prevent an obstacle from trapping the ending forever.
            if (blockedTime >= 3f)
            {
                Debug.LogWarning(
                    "[HistoricalFigure] Gandhi is blocked on the way " +
                    "to the door. Continuing to the Game Over sequence."
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
            yield return new WaitForSecondsRealtime(gameOverDelay);

        if (!Application.CanStreamedLevelBeLoaded(gameOverSceneName))
        {
            Debug.LogError(
                "[HistoricalFigure] Game Over scene '" +
                gameOverSceneName +
                "' cannot be loaded. Check the scene name and Build Settings."
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
        meetingMissed = true;

        HideOrderText();

        Debug.Log("[HistoricalFigure] MEETING MISSED.");

        if (cinematicCamera != null)
            cinematicCamera.ReturnToPlayer();

        ShowMeetingMissed();

        if (receivedWrongBeer)
        {
            endingTriggered = true;

            Debug.Log(
                "[HistoricalFigure] Wrong beer caused the missed meeting. " +
                "Triggering Game Win."
            );

            if (wrongBeerCompletion != null)
                wrongBeerCompletion.WrongBeerGiven();
            else
                Debug.LogError(
                    "[HistoricalFigure] WrongBeerLevelCompletion " +
                    "is not assigned in the Inspector."
                );
        }
        else
        {
            Debug.Log(
                "[HistoricalFigure] The correct-beer NPC missed the deadline."
            );
        }
    }

    private void ShowMeetingMissed()
    {
        if (resultShown)
            return;

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
            orderText.gameObject.SetActive(visible);

        if (orderTextPanel != null)
            orderTextPanel.SetActive(visible);
    }

    private void SetMeetingResultUIVisible(bool visible)
    {
        if (meetingResultText != null)
            meetingResultText.gameObject.SetActive(visible);

        if (meetingResultPanel != null)
            meetingResultPanel.SetActive(visible);
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
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (playerTransform == other.transform)
            {
                HideOrderText();
                playerTransform = null;
            }
        }
    }

    private void OnDisable()
    {
        if (orderTextCoroutine != null)
        {
            StopCoroutine(orderTextCoroutine);
            orderTextCoroutine = null;
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
    }
}