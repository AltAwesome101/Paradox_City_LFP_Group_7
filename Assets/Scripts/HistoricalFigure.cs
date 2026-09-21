using UnityEngine;
using TMPro;
using System.Collections;

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

    private Vector3 leaveDirection;

    public bool HasOrdered()
    {
        return hasOrdered;
    }

    public string GetRequestedBeer()
    {
        return requestedBeer;
    }

    public void PlaceOrder()
    {
        if (hasOrdered)
        {
            return;
        }

        hasOrdered = true;

        Debug.Log(
            "Historical Figure ordered: " +
            requestedBeer
        );

        if (orderText != null)
        {
            orderText.text =
                "Customer: I'll have a beer, please.";

            orderText.gameObject.SetActive(true);
        }

        // Do NOT start the meeting timer here.
        // The timer now starts when the beer is served.

        if (meetingResultText != null)
        {
            meetingResultText.gameObject.SetActive(false);
        }
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

    public void ServeBeer(Beer beer)
    {
        if (!hasOrdered)
        {
            return;
        }

        if (hasBeenServed)
        {
            return;
        }

        hasBeenServed = true;

        receivedWrongBeer =
            beer.IsWrongBeer();

        // Start the meeting timer ONLY after
        // the NPC receives the beer.
        if (meetingTimer != null)
        {
            meetingTimer.StartMeetingTimer();
        }

        // Start drinking.
        if (npcDrinking != null)
        {
            npcDrinking.StartDrinking(
                receivedWrongBeer
            );
        }

        // Start the cinematic camera.
        if (cinematicCamera != null)
        {
            cinematicCamera.FocusOnNPC(
                transform
            );
        }

        if (receivedWrongBeer)
        {
            Debug.Log(
                "WRONG BEER SERVED!"
            );

            Debug.Log(
                "The NPC does not know the beer is wrong."
            );

            if (orderText != null)
            {
                orderText.text =
                    "Customer: Thanks.";
            }
        }
        else
        {
            Debug.Log(
                "CORRECT BEER SERVED!"
            );

            if (orderText != null)
            {
                orderText.text =
                    "Customer: Thanks.";
            }
        }
    }

    private void Update()
    {
        // Do not process gameplay after the game
        // has been paused by the missed meeting.
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (meetingTimer != null &&
            meetingTimer.IsMeetingMissed() &&
            !meetingMissed)
        {
            meetingMissed = true;

            ShowMeetingMissed();

            if (cinematicCamera != null)
            {
                cinematicCamera.ReturnToPlayer();
            }

            return;
        }

        // Correct beer:
        // once the NPC finishes drinking, they leave.
        if (!receivedWrongBeer &&
            npcDrinking != null &&
            npcDrinking.HasFinishedDrinking() &&
            !meetingAttended &&
            !leaving)
        {
            AttendMeeting();
        }

        if (leaving)
        {
            LeaveBar();
        }
    }

    private void AttendMeeting()
    {
        if (meetingAttended)
        {
            return;
        }

        meetingAttended = true;

        Debug.Log(
            "HISTORICAL FIGURE FINISHED THE BEER " +
            "AND IS LEAVING FOR THE MEETING!"
        );

        if (meetingResultText != null)
        {
            meetingResultText.text =
                "HEADING TO MEETING";

            meetingResultText.gameObject.SetActive(true);

            StartCoroutine(
                HideMeetingResult()
            );
        }

        if (cinematicCamera != null)
        {
            cinematicCamera.ReturnToPlayer();
        }

        // Move away from the table/bar.
        leaveDirection =
            -transform.forward;

        leaveDirection.y = 0f;

        if (leaveDirection.sqrMagnitude < 0.01f)
        {
            leaveDirection = Vector3.forward;
        }

        leaveDirection.Normalize();

        leaving = true;
    }

    private void LeaveBar()
    {
        transform.position +=
            leaveDirection *
            leaveSpeed *
            Time.deltaTime;

        float distanceTravelled =
            Vector3.Distance(
                transform.position,
                transform.position +
                leaveDirection *
                leaveDistance
            );

        // Simple visual departure.
        // Stop after moving for enough time.
        if (distanceTravelled <= 0f)
        {
            leaving = false;
        }
    }

    private void ShowMeetingMissed()
    {
        if (resultShown)
        {
            return;
        }

        resultShown = true;

        Debug.Log(
            "HISTORICAL FIGURE MISSED THE MEETING!"
        );

        if (meetingResultText != null)
        {
            meetingResultText.text =
                "MEETING MISSED";

            meetingResultText.gameObject.SetActive(true);

            StartCoroutine(
                HideMeetingResult()
            );
        }
    }

    private IEnumerator HideMeetingResult()
    {
        yield return new WaitForSecondsRealtime(5f);

        if (meetingResultText != null)
        {
            meetingResultText.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer ==
            LayerMask.NameToLayer("Player"))
        {
            PlaceOrder();
        }
    }
}