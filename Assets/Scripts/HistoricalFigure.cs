using UnityEngine;
using TMPro;

public class HistoricalFigure : MonoBehaviour
{
    [Header("Order")]
    public string requestedBeer = "Correct Beer";

    [Header("Meeting")]
    public MeetingTimer meetingTimer;

    [Header("Order UI")]
    public TextMeshProUGUI orderText;

    private bool hasOrdered = false;
    private bool hasBeenServed = false;
    private bool receivedWrongBeer = false;
    private bool meetingMissed = false;

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

        receivedWrongBeer = beer.IsWrongBeer();

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer ==
            LayerMask.NameToLayer("Player"))
        {
            PlaceOrder();
        }
    }
}