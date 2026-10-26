using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class HistoricalFigure : MonoBehaviour
{
    [Header("Order")]
    public string requestedBeer = "Correct Beer";

    [Header("Delayed Tavern Entrance")]
    [Tooltip("Where Gandhi appears after waiting outside.")]
    public Transform entranceTarget;

    [Tooltip("Where Gandhi walks to sit at the table.")]
    public Transform sittingPoint;

    [Tooltip("Seconds after the level begins before Gandhi enters.")]
    public float entranceDelay = 30f;

    [Tooltip("Walking speed when entering the tavern.")]
    public float entranceSpeed = 2f;

    [Tooltip("Turning speed when entering the tavern.")]
    public float entranceTurnSpeed = 180f;

    [Tooltip("Distance at which Gandhi stops at the table.")]
    public float sittingArrivalDistance = 0.2f;

    [Tooltip("Duration of the simulated sitting movement.")]
    public float sitDownDuration = 1f;

    [Header("Meeting")]
    public MeetingTimer meetingTimer;

    [Header("Drinking")]
    public NPCDrinking npcDrinking;

    [Header("Order UI")]
    public TextMeshProUGUI orderText;
    public float orderTextDuration = 10f;
    public float orderTextDistance = 3f;

    [Header("Meeting Result UI")]
    public TextMeshProUGUI meetingResultText;

    [Header("Cinematic Camera")]
    public CinematicCameraFocus cinematicCamera;

    [Header("Wrong Beer Completion")]
    public WrongBeerLevelCompletion wrongBeerCompletion;

    [Header("NPC Door Movement")]
    [Tooltip("Create an empty GameObject at the tavern exit and assign it here.")]
    public Transform doorTarget;

    [Tooltip("NPC movement speed when leaving.")]
    public float leaveSpeed = 2f;

    [Tooltip("NPC turning speed in degrees per second.")]
    public float turnSpeed = 180f;

    [Tooltip("Distance at which the NPC stops at the door.")]
    public float doorArrivalDistance = 0.2f;

    [Tooltip("Optional correction for models whose forward axis is misaligned.")]
    public float modelRotationOffset = 0f;

    [Header("Game Over")]
    [Tooltip("Exact Game Over scene name from Build Settings.")]
    public string gameOverSceneName = "GameOver";

    [Tooltip("Seconds to wait before loading Game Over.")]
    public float gameOverDelay = 3f;

    private bool hasOrdered = false;
    private bool hasBeenServed = false;
    private bool receivedWrongBeer = false;
    private bool meetingMissed = false;
    private bool meetingAttended = false;
    private bool leaving = false;
    private bool resultShown = false;
    private bool endingTriggered = false;

    private bool canTakeOrder = false;
    private bool hasEnteredTavern = false;
    private bool entranceSetupValid = false;

    private Transform playerTransform;

    private Coroutine orderTextCoroutine;
    private Coroutine leaveCoroutine;
    private Coroutine entranceCoroutine;

    private Renderer[] characterRenderers;
    private bool[] originalRendererStates;

    private Collider[] characterColliders;
    private bool[] originalColliderStates;

    public bool HasOrdered()
    {
        return hasOrdered;
    }

    public string GetRequestedBeer()
    {
        return requestedBeer;
    }

    public bool HasBeenServed()
    {
        return hasBeenServed;
    }

    public bool ReceivedWrongBeer()
    {
        return receivedWrongBeer;
    }

    public bool MeetingWasMissed()
    {
        return meetingMissed;
    }

    public bool MeetingWasAttended()
    {
        return meetingAttended;
    }

    public bool CanTakeOrder()
    {
        return canTakeOrder && !hasOrdered &&
               !hasBeenServed && !endingTriggered;
    }

    private void Start()
    {
        if (orderText != null)
            orderText.gameObject.SetActive(false);

        if (meetingResultText != null)
            meetingResultText.gameObject.SetActive(false);

        // Preserve the original gameplay if the new markers
        // have not yet been assigned in the Inspector.
        if (entranceTarget == null || sittingPoint == null)
        {
            Debug.LogWarning(
                "[HistoricalFigure] Entrance Target or Sitting Point " +
                "is missing. Gandhi will remain at his original position. " +
                "Assign both markers to enable the delayed entrance."
            );

            canTakeOrder = true;
            hasEnteredTavern = true;
            return;
        }

        entranceSetupValid = true;

        // Cache the original visibility and collider states so
        // Gandhi can be hidden before entering the tavern.
        characterRenderers =
            GetComponentsInChildren<Renderer>(true);

        originalRendererStates =
            new bool[characterRenderers.Length];

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            originalRendererStates[i] =
                characterRenderers[i].enabled;
        }

        characterColliders =
            GetComponentsInChildren<Collider>(true);

        originalColliderStates =
            new bool[characterColliders.Length];

        for (int i = 0; i < characterColliders.Length; i++)
        {
            originalColliderStates[i] =
                characterColliders[i].enabled;
        }

        // Move Gandhi to the entrance and hide him until the
        // 30-second delay has elapsed.
        transform.position = entranceTarget.position;
        transform.rotation = entranceTarget.rotation;

        SetCharacterVisible(false);
        SetCharacterCollidersEnabled(false);

        canTakeOrder = false;
        hasEnteredTavern = false;

        entranceCoroutine = StartCoroutine(EnterTavernSequence());
    }

    private IEnumerator EnterTavernSequence()
    {
        // Wait before Gandhi enters the tavern.
        if (entranceDelay > 0f)
        {
            yield return new WaitForSeconds(entranceDelay);
        }

        if (entranceTarget == null || sittingPoint == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Entrance markers are missing."
            );

            yield break;
        }

        Debug.Log(
            "[HistoricalFigure] Gandhi is entering the tavern."
        );

        // Make Gandhi visible and restore his original collider states.
        SetCharacterVisible(true);
        SetCharacterCollidersEnabled(true);

        // Walk horizontally toward the sitting position.
        while (sittingPoint != null)
        {
            Vector3 targetPosition = sittingPoint.position;

            // Preserve the current height while walking.
            targetPosition.y = transform.position.y;

            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.magnitude <= sittingArrivalDistance)
                break;

            direction.Normalize();

            Quaternion targetRotation =
                Quaternion.LookRotation(direction, Vector3.up);

            targetRotation *= Quaternion.Euler(
                0f,
                modelRotationOffset,
                0f
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                entranceTurnSpeed * Time.deltaTime
            );

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                entranceSpeed * Time.deltaTime
            );

            yield return null;
        }

        // Simulate sitting by moving the main GameObject to
        // the designated seated position.
        Vector3 standingPosition = transform.position;
        Vector3 seatedPosition = sittingPoint.position;

        Quaternion seatedRotation =
            sittingPoint.rotation *
            Quaternion.Euler(0f, modelRotationOffset, 0f);

        float elapsed = 0f;

        if (sitDownDuration > 0f)
        {
            while (elapsed < sitDownDuration)
            {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / sitDownDuration
                );

                // Smoothly lower and rotate Gandhi into position.
                float smoothProgress =
                    progress * progress * (3f - 2f * progress);

                transform.position = Vector3.Lerp(
                    standingPosition,
                    seatedPosition,
                    smoothProgress
                );

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    seatedRotation,
                    smoothProgress
                );

                yield return null;
            }
        }

        transform.position = seatedPosition;
        transform.rotation = seatedRotation;

        hasEnteredTavern = true;
        canTakeOrder = true;
        entranceCoroutine = null;

        Debug.Log(
            "[HistoricalFigure] Gandhi is seated. " +
            "The player can now approach to take his order."
        );

        // If the player entered Gandhi's trigger while he was
        // arriving, allow the order once he is seated.
        TryPlaceOrderForNearbyPlayer();
    }

    private void SetCharacterVisible(bool visible)
    {
        if (characterRenderers == null)
            return;

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

    private void SetCharacterCollidersEnabled(bool enabled)
    {
        if (characterColliders == null)
            return;

        for (int i = 0; i < characterColliders.Length; i++)
        {
            if (characterColliders[i] != null)
            {
                characterColliders[i].enabled = enabled
                    ? originalColliderStates[i]
                    : false;
            }
        }
    }

    public void PlaceOrder()
    {
        if (!CanTakeOrder())
            return;

        hasOrdered = true;

        Debug.Log(
            "[HistoricalFigure] Gandhi ordered: " + requestedBeer
        );

        ShowOrderMessage(
            "Mahatma Gandhi: I'll have a beer, please."
        );

        if (meetingResultText != null)
            meetingResultText.gameObject.SetActive(false);
    }

    private void ShowOrderMessage(string message)
    {
        if (orderText == null)
            return;

        if (orderTextCoroutine != null)
        {
            StopCoroutine(orderTextCoroutine);
            orderTextCoroutine = null;
        }

        orderText.text = message;
        orderText.gameObject.SetActive(true);

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
        if (orderText != null)
            orderText.gameObject.SetActive(false);

        if (orderTextCoroutine != null)
        {
            StopCoroutine(orderTextCoroutine);
            orderTextCoroutine = null;
        }
    }

    private void Update()
    {
        // Hide the order or thank-you message when the player walks away.
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

        // Once Gandhi is seated, allow the order if the player
        // is already inside his interaction trigger.
        TryPlaceOrderForNearbyPlayer();

        if (endingTriggered)
            return;

        // Check the meeting deadline.
        if (meetingTimer != null &&
            meetingTimer.IsMeetingMissed() &&
            !meetingMissed)
        {
            HandleMeetingMissed();
            return;
        }

        // Correct beer finished before the deadline:
        // Gandhi leaves for the meeting.
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
            Debug.Log(
                "[HistoricalFigure] Gandhi is unaware of the mistake."
            );
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
            meetingResultText.gameObject.SetActive(true);
        }

        if (cinematicCamera != null)
            cinematicCamera.ReturnToPlayer();

        if (doorTarget == null)
        {
            Debug.LogError(
                "[HistoricalFigure] Door Target is not assigned. " +
                "Assign NPCDoorTarget in the Inspector."
            );

            StartCoroutine(LoadGameOverAfterDelay());
            return;
        }

        leaving = true;
        leaveCoroutine = StartCoroutine(WalkToDoor());
    }

    private IEnumerator WalkToDoor()
    {
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
                Quaternion.LookRotation(direction, Vector3.up);

            targetRotation *= Quaternion.Euler(
                0f,
                modelRotationOffset,
                0f
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                leaveSpeed * Time.deltaTime
            );

            yield return null;
        }

        leaving = false;

        Debug.Log("[HistoricalFigure] Gandhi reached the door.");

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
                "' cannot be loaded. Check the exact scene name " +
                "and Build Settings."
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

        // Wrong beer causes the missed-meeting Game Win sequence.
        if (receivedWrongBeer)
        {
            endingTriggered = true;

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
                    "[HistoricalFigure] WrongBeerLevelCompletion " +
                    "is not assigned. Assign it in the Inspector."
                );
            }
        }
        else
        {
            Debug.Log(
                "[HistoricalFigure] The correct-beer NPC missed the deadline. " +
                "The specified Game Over condition was not met."
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
            meetingResultText.gameObject.SetActive(true);

            StartCoroutine(HideMeetingResult());
        }
    }

    private IEnumerator HideMeetingResult()
    {
        yield return new WaitForSecondsRealtime(5f);

        if (meetingResultText != null)
            meetingResultText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerTransform = other.transform;

            // PlaceOrder will only work after Gandhi is seated.
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