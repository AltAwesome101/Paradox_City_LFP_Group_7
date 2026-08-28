using UnityEngine;

// Attach this to Hitler (or any NPC that needs to "see" the player).
// Generic - doesn't know anything about painting, levels, etc. It just answers
// one question every frame: can this NPC currently see the target?
public class NPCVision : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Usually the Player. If left empty, this will try to find the object tagged 'Player'.")]
    public Transform target;

    [Header("Vision Cone")]
    [Tooltip("How far this NPC can see, in metres")]
    public float viewDistance = 12f;
    [Tooltip("Full width of the vision cone, in degrees")]
    public float viewAngle = 90f;
    [Tooltip("Roughly eye height, so the raycast doesn't start at ground level")]
    public float eyeHeight = 1.6f;
    [Tooltip("Layers that block line of sight (walls, easels, furniture...). Do NOT include the Player's layer here.")]
    public LayerMask obstructionMask;

    [Header("Awareness")]
    [Tooltip("Seconds of continuous visibility before this NPC is fully alerted")]
    public float timeToSpot = 1.2f;
    [Tooltip("How quickly awareness fades once the target is out of sight (seconds to fully forget)")]
    public float timeToForget = 2f;

    [Header("Debug")]
    public bool debugLogging = false;
    public bool drawGizmo = true;

    public bool CanSeeTargetRightNow { get; private set; }
    public float Awareness { get; private set; } // 0 = no idea, 1 = fully spotted
    public bool HasSpottedTarget => Awareness >= 1f;

    // Fires the moment Awareness first reaches 1. Anything can subscribe to this
    // (PaintingCanvas uses it to kick the player out of an interaction).
    public System.Action OnTargetSpotted;

    private bool alreadyFiredSpotted = false;

    private void Awake()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    private void Update()
    {
        CanSeeTargetRightNow = target != null && CheckLineOfSight();

        if (CanSeeTargetRightNow)
        {
            Awareness += Time.deltaTime / Mathf.Max(0.01f, timeToSpot);
        }
        else
        {
            Awareness -= Time.deltaTime / Mathf.Max(0.01f, timeToForget);
        }
        Awareness = Mathf.Clamp01(Awareness);

        if (Awareness >= 1f && !alreadyFiredSpotted)
        {
            alreadyFiredSpotted = true;
            if (debugLogging) Debug.Log($"[NPCVision] {gameObject.name} spotted the target!");
            OnTargetSpotted?.Invoke();
        }
        else if (Awareness < 1f)
        {
            alreadyFiredSpotted = false; // lets it fire again next time awareness maxes out
        }
    }

    private bool CheckLineOfSight()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 toTarget = target.position - eyePos;
        float distance = toTarget.magnitude;

        if (distance > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > viewAngle * 0.5f) return false;

        // something solid between the NPC's eyes and the target blocks the sightline
        if (Physics.Raycast(eyePos, toTarget.normalized, out RaycastHit hit, distance, obstructionMask))
        {
            if (debugLogging) Debug.Log($"[NPCVision] Line of sight blocked by {hit.collider.name}");
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;
        Gizmos.color = CanSeeTargetRightNow ? Color.red : Color.yellow;
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Gizmos.DrawWireSphere(eyePos, viewDistance);

        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward * viewDistance;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward * viewDistance;
        Gizmos.DrawLine(eyePos, eyePos + left);
        Gizmos.DrawLine(eyePos, eyePos + right);
    }
}