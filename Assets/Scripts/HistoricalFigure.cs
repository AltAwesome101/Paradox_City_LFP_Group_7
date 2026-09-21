using UnityEngine;

public class HistoricalFigure : MonoBehaviour
{
    [Header("Order")]
    public string requestedBeer = "Correct Beer";

    private bool hasOrdered = false;

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlaceOrder();
        }
    }
}