using UnityEngine;
using TMPro;

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

    private bool hasOrdered = false;
    private bool hasBeenServed = false;
    private bool receivedWrongBeer = false;
    private bool meetingMissed = false;
    private bool meetingAttended = false;
    private bool resultShown = false;

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

        if (meetingTimer != null)
        {
            meetingTimer.StartMeetingTimer();
        }

        if (orderText != null)
        {
            orderText.text =
                "Customer: I'll have a beer, please.";

            orderText.gameObject.SetActive(true);
        }

        if (meetingResultText != null)
        {
            meetingResultText.text = "";
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

        if (npcDrinking != null)
        {
            npcDrinking.StartDrinking(
                receivedWrongBeer
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
        if (meetingTimer == null)
        {
            return;
        }

        // -----------------------------
        // MEETING MISSED
        // -----------------------------

        if (meetingTimer.IsMeetingMissed() &&
            !meetingMissed)
        {
            meetingMissed = true;

            ShowMeetingMissed();
        }

        // -----------------------------
        // CORRECT BEER
        // -----------------------------

        if (!receivedWrongBeer &&
            npcDrinking != null &&
            npcDrinking.HasFinishedDrinking() &&
            !meetingMissed &&
            !meetingAttended)
        {
            AttendMeeting();
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
            "HISTORICAL FIGURE ATTENDED THE MEETING!"
        );

        if (meetingResultText != null)
        {
            meetingResultText.text =
                "MEETING ATTENDED";

            meetingResultText.gameObject.SetActive(true);
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