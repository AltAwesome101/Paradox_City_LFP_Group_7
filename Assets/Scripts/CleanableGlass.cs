using UnityEngine;

public class CleanableGlass : MonoBehaviour
{
    [Header("Glass Settings")]
    public string glassName = "Glass";
    public bool isCollected = false;
    public bool isReturned = false;

    private Rigidbody rb;
    private Collider[] glassColliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        glassColliders = GetComponentsInChildren<Collider>(true);

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void PickUp(Transform holdPoint)
    {
        if (holdPoint == null || isCollected || isReturned)
        {
            return;
        }

        isCollected = true;

        rb.isKinematic = true;
        rb.useGravity = false;

        if (glassColliders != null)
        {
            foreach (Collider glassCollider in glassColliders)
            {
                if (glassCollider != null)
                {
                    glassCollider.enabled = false;
                }
            }
        }

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Debug.Log(glassName + " picked up successfully.");
    }

    public void ReturnToCounter(Transform counterPoint)
    {
        if (counterPoint == null || !isCollected || isReturned)
        {
            return;
        }

        isCollected = false;
        isReturned = true;

        transform.SetParent(null);
        transform.SetPositionAndRotation(
            counterPoint.position,
            counterPoint.rotation
        );

        rb.isKinematic = true;
        rb.useGravity = false;

        if (glassColliders != null)
        {
            foreach (Collider glassCollider in glassColliders)
            {
                if (glassCollider != null)
                {
                    glassCollider.enabled = true;
                }
            }
        }

        if (GlassCleaningProgress.Instance != null)
        {
            GlassCleaningProgress.Instance.RegisterReturnedGlass(this);
        }
        else
        {
            Debug.LogWarning(
                "GlassCleaningProgress manager was not found in the scene."
            );
        }

        Debug.Log(glassName + " has been returned to the counter.");
    }
}