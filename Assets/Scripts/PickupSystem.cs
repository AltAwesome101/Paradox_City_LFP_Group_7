using TMPro;
using UnityEngine;

public class PickupSystem : MonoBehaviour
{
    [Header("Pickup")]
    public Camera playerCamera;
    public float pickupDistance = 3f;

    [Header("UI")]
    public GameObject pickupTextObject;
    public TextMeshProUGUI pickupText;

    private void Update()
    {
        CheckForPickup();
    }

    private void CheckForPickup()
    {
        // Ray starts from the centre of the player's camera
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        // Check what the player is looking at
        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            Beer beer = hit.collider.GetComponent<Beer>();

            if (beer != null)
            {
                ShowPickupMessage("[E] Pick Up Beer");

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpBeer(beer);
                }

                return;
            }
        }

        HidePickupMessage();
    }

    private void PickUpBeer(Beer beer)
    {
        Debug.Log("Picked up: " + beer.beerName);

        // Temporarily make the beer disappear
        beer.gameObject.SetActive(false);

        HidePickupMessage();
    }

    private void ShowPickupMessage(string message)
    {
        if (pickupTextObject != null)
        {
            pickupTextObject.SetActive(true);
        }

        if (pickupText != null)
        {
            pickupText.text = message;
        }
    }

    private void HidePickupMessage()
    {
        if (pickupTextObject != null)
        {
            pickupTextObject.SetActive(false);
        }
    }
}