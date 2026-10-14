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

    [Header("Meeting Result UI")]
    public TextMeshProUGUI meetingResultText;

    [Header("Cinematic Camera")]
    public CinematicCameraFocus cinematicCamera;

    [Header("Wrong Beer Completion")]
    public WrongBeerLevelCompletion wrongBeerCompletion;

    [Header("Game Over")]
    [Tooltip("Exact Game Over scene name from Build Settings.")]
    public string gameOverSceneName = "GameOver";

    [Tooltip("Seconds to display the result before loading Game Over.")]
    public float gameOverDelay = 3f;

    [Header("Leaving")]
    public float leaveSpeed = 2f;
    public float leaveDistance = 8f;

    private bool hasOrdered = false;
    private bool hasBeenServed = false;
    private bool receivedWrongBeer = false;
    private bool meetingMissed = false;
    private bool meetingAttended = false;
    private bool leaving = false;
    private bool resultShown = false;
    private bool endingTriggered = false;

    private Vector3 leaveDirection;
    private Vector3 leaveStartPosition;

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

        Debug.Log(
            "[HistoricalFigure] Ordered: " + requestedBeer
        );

        if (orderText != null)
        {
            orderText.text = "Customer: I'll have a beer, please.";
            orderText.gameObject.SetActive(true);
        }

        if (meetingResultText != null)
            meetingResultText.gameObject.SetActive(false);
    }

    public void ServeBeer(Beer beer)
    {
        if (!hasOrdered || hasBeenServed || beer == null)
            return;

        hasBeenServed = true;
        receivedWrongBeer = beer.IsWrongBeer();

        // The meeting timer starts only when the beer is served.
        if (meetingTimer != null)
            meetingTimer.StartMeetingTimer();

        if (npcDrinking != null)
            npcDrinking.StartDrinking(receivedWrongBeer);

        // Lock the camera on the NPC during drinking.
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

        if (orderText != null)
            orderText.text = "Customer: Thanks.";
    }

    private void Update()
    {
        if (endingTriggered)
            return;

        // Check whether the deadline has expired first.
        if (meetingTimer != null &&
            meetingTimer.IsMeetingMissed() &&
            !meetingMissed)
        {
            HandleMeetingMissed();
            return;
        }

        // Correct beer: finishing before the deadline means
        // the NPC attends the meeting and Game Over follows.
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

        if (leaving)
            LeaveBar();
    }

    private void AttendMeeting()
    {
        if (meetingAttended || endingTriggered)
            return;

        meetingAttended = true;
        endingTriggered = true;

        Debug.Log(
            "[HistoricalFigure] Correct beer finished in time. " +
            "NPC attends the meeting. GAME OVER."
        );

        if (meetingResultText != null)
        {
            meetingResultText.text = "HEADING TO MEETING";
            meetingResultText.gameObject.SetActive(true);
        }

        if (cinematicCamera != null)
            cinematicCamera.ReturnToPlayer();

        leaveDirection = -transform.forward;
        leaveDirection.y = 0f;

        if (leaveDirection.sqrMagnitude < 0.01f)
            leaveDirection = Vector3.forward;

        leaveDirection.Normalize();
        leaveStartPosition = transform.position;
        leaving = true;

        StartCoroutine(LoadGameOver());
    }

    private void HandleMeetingMissed()
    {
        meetingMissed = true;

        Debug.Log("[HistoricalFigure] MEETING MISSED.");

        if (cinematicCamera != null)
            cinematicCamera.ReturnToPlayer();

        ShowMeetingMissed();

        // Only the wrong-beer outcome completes the level
        // through the Game Win -> FutureScene sequence.
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

    private IEnumerator LoadGameOver()
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

    private void LeaveBar()
    {
        transform.position +=
            leaveDirection * leaveSpeed * Time.deltaTime;

        float distanceTravelled =
            Vector3.Distance(leaveStartPosition, transform.position);

        if (distanceTravelled >= leaveDistance)
            leaving = false;
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
            PlaceOrder();
    }
}