using UnityEngine;
using GameDevExtensionMethods;
/// <summary>
/// Predicts an apple's landing point under constant gravity (SUVAT, no drag/bounce)
/// and visualizes it: a line renderer for the arc, and a ground marker on the
/// actual ground surface at the predicted landing point.
/// </summary>
public class AppleTrajectoryProjector : MonoBehaviour
{
    [SerializeField] GameObject groundMarker;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] int lineSegments = 20;
    [SerializeField] float markerRadius = 0.5f;

    const float RaycastLookAheadHeight = 100f;

    struct TrajectoryPrediction
    {
        public Vector3 LandingPoint;
        public Vector3 GroundNormal;
        public float TimeToLand;
        public bool IsValid;
    }

    void Awake()
    {
        groundMarker.transform.SetParent(null, true);
    }

    public void Project(Transform appleTransform, Rigidbody appleRigidbody, LayerMask groundMask)
    {
        var prediction = Predict(appleTransform.position, appleRigidbody.linearVelocity, groundMask);

        if (!prediction.IsValid)
        {
            Hide();
            return;
        }

        ApplyMarker(prediction);
        ApplyLine(appleTransform.position, appleRigidbody.linearVelocity, prediction.TimeToLand);
    }

    TrajectoryPrediction Predict(Vector3 origin, Vector3 velocity, LayerMask groundMask)
    {
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit below, Mathf.Infinity, groundMask))
            return default;

        float gravity = Mathf.Abs(Physics.gravity.y);
        float timeToLand = SolveTimeToHeight(origin.y, velocity.y, below.point.y, gravity);
        if (timeToLand < 0f)
            return default;

        Vector3 predictedXZ = origin + velocity.Flatten() * timeToLand; // Flatten() from GameDevExtensionMethods

        // Terrain under the landing point may differ from terrain directly below the
        // apple (slope, ledge) - re-cast there, falling back to the first hit if it misses.
        RaycastHit landingHit = Physics.Raycast(
            predictedXZ + Vector3.up * RaycastLookAheadHeight, Vector3.down,
            out RaycastHit hit, Mathf.Infinity, groundMask) ? hit : below;

        return new TrajectoryPrediction
        {
            LandingPoint = landingHit.point,
            GroundNormal = landingHit.normal,
            TimeToLand = timeToLand,
            IsValid = true
        };
    }

    float SolveTimeToHeight(float startY, float startVelocityY, float targetY, float gravity)
    {
        // 0.5*g*t^2 - v0y*t - (startY - targetY) = 0
        float a = 0.5f * gravity;
        float b = -startVelocityY;
        float c = -(startY - targetY);
        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return -1f;
        return (-b + Mathf.Sqrt(discriminant)) / (2f * a);
    }

    void ApplyMarker(TrajectoryPrediction prediction)
    {
        groundMarker.SetActive(true);
        groundMarker.transform.position = prediction.LandingPoint;
        groundMarker.transform.up = prediction.GroundNormal;
        groundMarker.transform.localScale = new Vector3(
            markerRadius * 2f,
            groundMarker.transform.localScale.y,
            markerRadius * 2f
        );
    }

    void ApplyLine(Vector3 origin, Vector3 velocity, float duration)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);
        lineRenderer.positionCount = lineSegments + 1;

        for (int i = 0; i <= lineSegments; i++)
        {
            float t = duration * i / lineSegments;
            Vector3 point = origin + velocity.Flatten() * t;
            point.y = origin.y + velocity.y * t - 0.5f * gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }

    public void Hide()
    {
        groundMarker.SetActive(false);
        lineRenderer.positionCount = 0;
    }
}