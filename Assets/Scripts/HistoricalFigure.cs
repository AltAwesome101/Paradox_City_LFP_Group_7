using UnityEngine;
using TMPro;

public class HistoricalFigure : MonoBehaviour
{
    [Header("Order")]
    public string requestedBeer = "Correct Beer";

    [Header("Order UI")]
    public TextMeshProUGUI orderText;

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

        if (orderText != null)
        {
            orderText.text =
                "Customer: I'll have a beer, please.";

            orderText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlaceOrder();
        }
    }
}