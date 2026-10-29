using UnityEngine;

public class FloatingGlassArrow : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverHeight = 0.2f;
    public float hoverSpeed = 2f;

    [Header("Glass Reference")]
    public CleanableGlass targetGlass;

    private Vector3 startingPosition;

    private void Start()
    {
        startingPosition = transform.position;
    }

    private void Update()
    {
        // Hide the arrow as soon as its glass is picked up.
        if (targetGlass != null && targetGlass.isCollected)
        {
            gameObject.SetActive(false);
            return;
        }

        // Keep the arrow hovering.
        float newY = startingPosition.y +
                     Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;

        transform.position = new Vector3(
            startingPosition.x,
            newY,
            startingPosition.z
        );
    }
}