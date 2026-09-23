using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


[RequireComponent(typeof(NavMeshAgent))]
public class NPCPatrol : MonoBehaviour
{
    [Tooltip("Points this NPC walks between. Filled in automatically by BarNPCSpawner, " +
             "or set these yourself if you're placing an NPC by hand.")]
    public Transform[] points;

    [Tooltip("If true, picks the next point randomly each time instead of going through them in order.")]
    [SerializeField] bool randomOrder = false;

    [Tooltip("Walk speed for this NPC - lower than NavMeshAgent's default for a slow, casual pace.")]
    [SerializeField] float walkSpeed = 1f;

    [Tooltip("How close counts as \"arrived\" before pausing / picking the next point.")]
    [SerializeField] float arriveThreshold = 0.3f;

    [Tooltip("How long to stand still at each point before walking to the next one.")]
    [SerializeField] float waitAtPoint = 2f;

    [Tooltip("How far to search for a valid NavMesh point near each target, in case it isn't exactly on the baked mesh.")]
    [SerializeField] float navMeshSampleRadius = 2f;

    NavMeshAgent agent;
    int currentIndex = -1;
    float waitTimer;
    bool waiting;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = walkSpeed;
    }

    void Start()
    {
        GoToNextPoint();
    }

    void Update()
    {
        if (points == null || points.Length == 0) return;

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                GoToNextPoint();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= arriveThreshold)
        {
            waiting = true;
            waitTimer = waitAtPoint;
        }
    }

    void GoToNextPoint()
    {
        if (points == null || points.Length == 0) return;

        currentIndex = randomOrder
            ? Random.Range(0, points.Length)
            : (currentIndex + 1) % points.Length;

        Transform target = points[currentIndex];
        if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }
}