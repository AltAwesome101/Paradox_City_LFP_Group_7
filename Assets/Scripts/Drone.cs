using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Drone : MonoBehaviour
{
    [Header("Flight Area")]
    [Tooltip("Center of the box this drone wanders inside.")]
    public Vector3 areaCenter;

    [Tooltip("Full width/height/depth of the wander box.")]
    public Vector3 areaSize = new Vector3(60f, 12f, 60f);

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float turnSpeed = 90f;

    [Tooltip("Pick a new random destination at least this often, even if the current one hasn't been reached.")]
    public float wanderPointInterval = 6f;

    [Tooltip("How close counts as 'arrived' at the current destination.")]
    public float arriveDistance = 1.5f;

    [Header("Obstacle Avoidance")]
    [Tooltip("Layers the drone must not fly through (buildings, terrain, props, etc). Do NOT include the Player layer here - use Player Avoidance below for that.")]
    public LayerMask obstacleLayers;

    [Tooltip("How far ahead the drone looks for obstacles.")]
    public float obstacleDetectRange = 5f;

    [Tooltip("Radius of the drone's body for collision purposes - keep roughly matching the prefab's actual size.")]
    public float obstacleAvoidRadius = 0.75f;

    [Header("Player Avoidance")]
    [Tooltip("Leave empty to auto-find the GameObject tagged 'Player' at Awake.")]
    public Transform player;

    [Tooltip("Drone steers away once the player gets this close.")]
    public float minPlayerDistance = 6f;

    [Header("Debug")]
    public bool debugLogging = false;
    public bool drawGizmos = true;

    private Rigidbody rb;
    private Vector3 targetPoint;
    private float wanderTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        
        rb.isKinematic = true;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
            else if (debugLogging)
                Debug.LogWarning("[Drone] No GameObject tagged 'Player' found - player avoidance disabled.");
        }

        if (areaCenter == Vector3.zero)
            areaCenter = transform.position;

        PickNewWanderPoint();
    }

    private void FixedUpdate()
    {
        wanderTimer += Time.fixedDeltaTime;

        bool arrived = Vector3.Distance(transform.position, targetPoint) < arriveDistance;
        if (arrived || wanderTimer >= wanderPointInterval)
        {
            PickNewWanderPoint();
            wanderTimer = 0f;
        }

        Vector3 desiredDir = (targetPoint - transform.position).normalized;

        
        if (TryGetAvoidanceDirection(out Vector3 avoidDir))
        {
            desiredDir = Vector3.Lerp(desiredDir, avoidDir, 0.85f).normalized;

            if (debugLogging)
                Debug.Log($"[Drone:{name}] Avoiding obstacle.");
        }

        // --- Player avoidance ---
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer < minPlayerDistance)
            {
                Vector3 away = (transform.position - player.position);
                away.y = 0f; 
                away.Normalize();

                float strength = Mathf.Clamp01((minPlayerDistance - distToPlayer) / minPlayerDistance);
                desiredDir = Vector3.Lerp(desiredDir, away, strength).normalized;

                if (debugLogging)
                    Debug.Log($"[Drone:{name}] Steering away from player, dist={distToPlayer:F1}");
            }
        }

        
        desiredDir = ClampToArea(desiredDir);

        
        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }

        rb.MovePosition(rb.position + transform.forward * moveSpeed * Time.fixedDeltaTime);
    }

    
    private bool TryGetAvoidanceDirection(out Vector3 avoidDir)
    {
        if (Physics.SphereCast(transform.position, obstacleAvoidRadius, transform.forward,
                out RaycastHit hit, obstacleDetectRange, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            
            Vector3 steer = Vector3.ProjectOnPlane(transform.forward, hit.normal) + hit.normal * 0.5f;
            avoidDir = steer.normalized;
            return true;
        }

        avoidDir = Vector3.zero;
        return false;
    }

    
    private Vector3 ClampToArea(Vector3 desiredDir)
    {
        Vector3 half = areaSize * 0.5f;
        Vector3 local = transform.position - areaCenter;

        Vector3 correction = Vector3.zero;
        if (Mathf.Abs(local.x) > half.x) correction.x = -Mathf.Sign(local.x);
        if (Mathf.Abs(local.y) > half.y) correction.y = -Mathf.Sign(local.y);
        if (Mathf.Abs(local.z) > half.z) correction.z = -Mathf.Sign(local.z);

        if (correction == Vector3.zero)
            return desiredDir;

        return Vector3.Lerp(desiredDir, correction.normalized, 0.9f).normalized;
    }

    private void PickNewWanderPoint(int attempts = 12)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = areaCenter + new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                Random.Range(-areaSize.y / 2f, areaSize.y / 2f),
                Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            );

            if (!Physics.CheckSphere(candidate, obstacleAvoidRadius, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                targetPoint = candidate;
                return;
            }
        }

        
        targetPoint = areaCenter;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Vector3 center = areaCenter == Vector3.zero ? transform.position : areaCenter;

        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        Gizmos.DrawWireCube(center, areaSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPoint, 0.5f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minPlayerDistance);
    }
}