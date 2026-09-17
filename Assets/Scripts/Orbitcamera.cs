using UnityEngine;


[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    [Header("Orbit Settings")]
    [Tooltip("The point the camera orbits around, e.g. the center of your city model")]
    public Transform orbitTarget;

    [Tooltip("Distance from the target")]
    public float distance = 20f;

    [Tooltip("Height above the target")]
    public float height = 8f;

    [Tooltip("Degrees per second the camera orbits")]
    public float orbitSpeed = 10f;

    private float currentAngle = 0f;

    void OnEnable()
    {
        
        currentAngle = 0f;
    }

    void Update()
    {
        if (orbitTarget == null)
        {
            Debug.LogWarning("OrbitCamera has no Orbit Target assigned.");
            return;
        }

        
        currentAngle += orbitSpeed * Time.unscaledDeltaTime;

        float radians = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * distance;
        offset.y = height;

        transform.position = orbitTarget.position + offset;
        transform.LookAt(orbitTarget.position + Vector3.up * (height * 0.3f));
    }
}