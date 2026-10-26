using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class HistoricalFigure : MonoBehaviour
{
    [Header("Order")]
    public string requestedBeer = "Correct Beer";

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

    [Tooltip("NPC movement speed.")]
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

    private Transform playerTransform;
    private Coroutine orderTextCoroutine;
    private Coroutine leaveCoroutine;

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

    public void PlaceOrder()
    {
        if (hasOrdered)
            return;

        hasOrdered = true;

        Debug.Log("[HistoricalFigure] Ordered: " + requestedBeer);

        ShowOrderMessage("Customer: I'll have a beer, please.");

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
        // the NPC leaves for the meeting.
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

    public void ServeBeer(Beer beer)
    {
        if (!hasOrdered || hasBeenServed || beer == null)
            return;

        hasBeenServed = true;
        receivedWrongBeer = beer.IsWrongBeer();

        ShowOrderMessage("Customer: Thanks.");

        if (meetingTimer != null)
            meetingTimer.StartMeetingTimer();

        if (npcDrinking != null)
            npcDrinking.StartDrinking(receivedWrongBeer);

        if (cinematicCamera != null)
            cinematicCamera.FocusOnNPC(transform);

        if (receivedWrongBeer)
        {
            Debug.Log("[HistoricalFigure] WRONG BEER SERVED.");
            Debug.Log("[HistoricalFigure] NPC is unaware of the mistake.");
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
            "NPC is leaving for the meeting."
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
            // Use the door's world position as the destination.
            Vector3 targetPosition = doorTarget.position;

            // Keep the NPC at its existing vertical height.
            targetPosition.y = transform.position.y;

            // Calculate the horizontal direction to the door.
            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            // Stop when the NPC reaches the door.
            if (direction.magnitude <= doorArrivalDistance)
                break;

            direction.Normalize();

            // Rotate toward the direction of travel.
            Quaternion targetRotation =
                Quaternion.LookRotation(direction, Vector3.up);

            // Apply an optional correction if the model faces
            // sideways or backward relative to Unity's forward axis.
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

            // Move toward the door target, not the NPC's forward axis.
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                leaveSpeed * Time.deltaTime
            );

            yield return null;
        }

        leaving = false;

        Debug.Log("[HistoricalFigure] NPC reached the door.");

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
                "' cannot be loaded. Check the exact scene name and Build Settings."
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
                    "[HistoricalFigure] WrongBeerLevelCompletion is not assigned. " +
                    "Assign it in the Inspector."
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
    }
}