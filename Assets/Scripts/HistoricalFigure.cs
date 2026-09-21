using UnityEngine;
using TMPro;

public class HistoricalFigure : MonoBehaviour
{
    [Header("Order")]
    public string requestedBeer = "Correct Beer";

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

            meetingMissed = true;

            Debug.Log(
                "HISTORICAL EVENT CHANGED: " +
                "The meeting was missed."
            );

            if (orderText != null)
            {
                orderText.text =
                    "Customer: Uh... this isn't what I ordered.";
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
                    "Customer: Thanks. That's perfect.";
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