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
    [Tooltip("Seconds Hitler pauses at each patrol point before moving to the next")]
    public float patrolPauseDuration = 2f;
    [Tooltip("How close counts as 'arrived' at a destination")]
    public float arriveDistance = 0.3f;

    [Header("Optional Animation")]
    [Tooltip("Leave empty if you haven't set up animations yet - everything below just won't fire")]
    public Animator animator;
    public string smileTrigger = "Smile";
    public string happyTrigger = "Happy";
    public string walkBool = "IsWalking";

    public State CurrentState { get; private set; } = State.Idle;

    private NavMeshAgent agent;
    private int currentPatrolIndex = 0;
    private float stateTimer = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case State.WalkingToEasel:
                UpdateWalkBool(true);
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
                UpdateWalkBool(agent.velocity.sqrMagnitude > 0.05f);
                if (patrolPoints != null && patrolPoints.Length > 0 && HasArrived())
                {
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) GoToNextPatrolPoint();
                }
                break;

            case State.Celebrating:
                UpdateWalkBool(false);
                break;
        }
    }

    private void UpdateWalkBool(bool walking)
    {
        if (animator != null && !string.IsNullOrEmpty(walkBool))
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

        if (animator != null && !string.IsNullOrEmpty(smileTrigger))
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
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void GoToNextPatrolPoint()
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        stateTimer = patrolPauseDuration;
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

        if (animator != null && !string.IsNullOrEmpty(happyTrigger))
            animator.SetTrigger(happyTrigger);
    }
}