using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HitlerNPC : MonoBehaviour
{
    public enum State { Idle, WalkingToEasel, AdmiringPainting, Patrolling, Celebrating }

    [Header("Key Points")]
    [Tooltip("Where the easel/painting is - Hitler walks here at the start of the level")]
    public Transform easelPoint;
    [Tooltip("Points Hitler wanders between once he's finished admiring the painting")]
    public Transform[] patrolPoints;

    [Header("Timing")]
    [Tooltip("Seconds Hitler spends looking at the painting before he starts wandering")]
    public float admireDuration = 3f;
    [Tooltip("Random range (min, max) for how long Hitler pauses at each patrol point")]
    public Vector2 patrolPauseRange = new Vector2(1.5f, 3.5f);
    [Tooltip("How close counts as 'arrived' at a destination")]
    public float arriveDistance = 0.3f;

    [Header("Movement / Facing")]
    [Tooltip("Degrees per second Hitler turns to face his direction of travel")]
    public float turnSpeed = 240f;

    [Header("Look-Around (while paused at a patrol point)")]
    [Tooltip("How far Hitler sweeps his gaze left/right from his base heading, in degrees")]
    public float lookAroundAngle = 55f;
    [Tooltip("How fast the look-around sweep oscillates")]
    public float lookAroundSpeed = 0.6f;

    [Header("Optional Animation")]
    [Tooltip("Leave empty if you haven't set up animations yet - everything below just won't fire")]
    public Animator animator;
    public string smileTrigger = "Smile";
    public string happyTrigger = "Happy";
    public string walkBool = "IsWalking";

    [Header("Debug")]
    public bool debugLogging = true;

    public State CurrentState { get; private set; } = State.Idle;

    private NavMeshAgent agent;
    private int currentPatrolIndex = 0;
    private float stateTimer = 0f;

    private bool hasCapturedPauseYaw;
    private float pausedBaseYaw;

    private System.Collections.Generic.HashSet<string> animatorParams;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // We handle rotation ourselves (facing + look-around sweep), so the
        // agent should only ever move the position.
        agent.updateRotation = false;

        CacheAnimatorParameters();
    }

    // Reads the actual parameter list off the assigned Animator Controller so
    // we never call SetBool/SetTrigger on a parameter that doesn't exist -
    // that's what was throwing "Parameter 'IsWalking' does not exist".
    private void CacheAnimatorParameters()
    {
        animatorParams = new System.Collections.Generic.HashSet<string>();

        if (animator == null)
            return;

        if (animator.runtimeAnimatorController == null)
        {
            if (debugLogging)
                Debug.LogWarning("[HitlerNPC] Animator is assigned but has no Animator Controller - " +
                                  "animation calls will be silently skipped until one is set up.");
            return;
        }

        foreach (AnimatorControllerParameter p in animator.parameters)
            animatorParams.Add(p.name);

        WarnIfMissing(walkBool, "Bool");
        WarnIfMissing(smileTrigger, "Trigger");
        WarnIfMissing(happyTrigger, "Trigger");
    }

    private void WarnIfMissing(string paramName, string expectedType)
    {
        if (!debugLogging || string.IsNullOrEmpty(paramName))
            return;

        if (!animatorParams.Contains(paramName))
        {
            Debug.LogWarning($"[HitlerNPC] Animator Controller has no '{expectedType}' parameter named " +
                              $"'{paramName}' - add it in the Animator window (Parameters tab) if you want this to animate.");
        }
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case State.WalkingToEasel:
                FaceMovementDirection();
                UpdateWalkBool(agent.velocity.sqrMagnitude > 0.05f);

                if (HasArrived())
                {
                    BeginAdmiring();
                }
                break;

            case State.AdmiringPainting:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    BeginPatrolling();
                }
                break;

            case State.Patrolling:
                UpdatePatrolling();
                break;

            case State.Celebrating:
                UpdateWalkBool(false);
                break;
        }
    }

    private void UpdatePatrolling()
    {
        bool hasPoints = patrolPoints != null && patrolPoints.Length > 0;
        bool arrived = hasPoints && HasArrived();

        if (!arrived)
        {
            FaceMovementDirection();
            UpdateWalkBool(agent.velocity.sqrMagnitude > 0.05f);
            hasCapturedPauseYaw = false;
            return;
        }

        UpdateWalkBool(false);

        // The moment Hitler settles at a patrol point, lock in the heading
        // he'll sweep his gaze around, and roll a random pause duration.
        if (!hasCapturedPauseYaw)
        {
            hasCapturedPauseYaw = true;
            pausedBaseYaw = transform.eulerAngles.y;
            stateTimer = Random.Range(patrolPauseRange.x, patrolPauseRange.y);
        }

        float sweep = Mathf.Sin(Time.time * lookAroundSpeed) * lookAroundAngle;
        transform.rotation = Quaternion.Euler(0f, pausedBaseYaw + sweep, 0f);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            GoToNextPatrolPoint();
        }
    }

    // Turns the NPC to face wherever the NavMeshAgent is currently trying to go.
    private void FaceMovementDirection()
    {
        Vector3 dir = agent.velocity;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void UpdateWalkBool(bool walking)
    {
        if (animator == null || string.IsNullOrEmpty(walkBool))
            return;

        if (!animatorParams.Contains(walkBool))
            return; // already warned about this once, in Awake

        animator.SetBool(walkBool, walking);
    }

    private bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= arriveDistance;
    }

    public void BeginWalkingToEasel()
    {
        if (easelPoint == null)
        {
            Debug.LogError("[HitlerNPC] No easelPoint assigned - can't start the level.");
            return;
        }

        CurrentState = State.WalkingToEasel;
        agent.isStopped = false;
        agent.SetDestination(easelPoint.position);
    }

    private void BeginAdmiring()
    {
        CurrentState = State.AdmiringPainting;
        agent.isStopped = true;
        UpdateWalkBool(false);

        if (easelPoint != null)
        {
            Vector3 lookPos = easelPoint.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }

        if (animator != null && !string.IsNullOrEmpty(smileTrigger) && animatorParams.Contains(smileTrigger))
            animator.SetTrigger(smileTrigger);

        stateTimer = admireDuration;
    }

    private void BeginPatrolling()
    {
        CurrentState = State.Patrolling;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        currentPatrolIndex = 0;
        hasCapturedPauseYaw = false;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void GoToNextPatrolPoint()
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        hasCapturedPauseYaw = false;
    }

    public void Celebrate()
    {
        CurrentState = State.Celebrating;
        agent.isStopped = true;

        if (easelPoint != null)
        {
            Vector3 lookPos = easelPoint.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }

        if (animator != null && !string.IsNullOrEmpty(happyTrigger) && animatorParams.Contains(happyTrigger))
            animator.SetTrigger(happyTrigger);
    }
}