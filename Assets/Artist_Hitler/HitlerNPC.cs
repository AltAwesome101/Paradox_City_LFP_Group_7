using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class HitlerNPC : MonoBehaviour
{
    public enum State
    {
        Idle,
        WalkingToEasel,
        AdmiringPainting,
        Patrolling,
        Celebrating
    }

    [Header("Paintings / Easels")]
    [Tooltip("All paintings/easels that Hitler will visit.")]
    public Transform[] easelPoints;

    [Tooltip("If enabled, Hitler will visit every painting once before starting random movement.")]
    public bool visitAllPaintingsFirst = true;

    [Header("Timing")]
    [Tooltip("Seconds Hitler spends looking at each painting.")]
    public float admireDuration = 3f;

    [Tooltip("Random extra time to wait before moving to another painting.")]
    public Vector2 paintingPauseRange = new Vector2(1f, 3f);

    [Tooltip("How close counts as 'arrived' at a destination.")]
    public float arriveDistance = 0.3f;

    [Header("Random Painting Movement")]
    [Tooltip("If enabled, Hitler will never select the same painting twice in a row.")]
    public bool preventSamePaintingTwice = true;

    [Header("Movement / Facing")]
    [Tooltip("Degrees per second Hitler turns to face his direction of travel.")]
    public float turnSpeed = 240f;

    [Header("Look-Around")]
    [Tooltip("How far Hitler sweeps his gaze left/right from his base heading.")]
    public float lookAroundAngle = 55f;

    [Tooltip("How fast the look-around sweep oscillates.")]
    public float lookAroundSpeed = 0.6f;

    [Header("Optional Animation")]
    [Tooltip("Leave empty if you haven't set up animations yet.")]
    public Animator animator;

    public string smileTrigger = "Smile";
    public string happyTrigger = "Happy";
    public string walkBool = "IsWalking";

    [Header("Debug")]
    public bool debugLogging = true;

    public State CurrentState { get; private set; } = State.Idle;

    private NavMeshAgent agent;

    private float stateTimer = 0f;

    
    private int currentPaintingIndex = -1;

    
    private int previousPaintingIndex = -1;

    
    private int nextInitialPaintingIndex = 0;

    
    private bool completedInitialPaintingVisits = false;

    
    private bool hasCapturedPauseYaw;
    private float pausedBaseYaw;

    private HashSet<string> animatorParams;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        
        agent.updateRotation = false;

        CacheAnimatorParameters();
    }

    private void CacheAnimatorParameters()
    {
        animatorParams = new HashSet<string>();

        if (animator == null)
            return;

        if (animator.runtimeAnimatorController == null)
        {
            if (debugLogging)
            {
                Debug.LogWarning(
                    "[HitlerNPC] Animator is assigned but has no Animator Controller."
                );
            }

            return;
        }

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            animatorParams.Add(p.name);
        }

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
            Debug.LogWarning(
                $"[HitlerNPC] Animator Controller has no '{expectedType}' parameter named " +
                $"'{paramName}'."
            );
        }
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case State.WalkingToEasel:

                FaceMovementDirection();

                UpdateWalkBool(
                    agent.velocity.sqrMagnitude > 0.05f
                );

                if (HasArrived())
                {
                    BeginAdmiring();
                }

                break;


            case State.AdmiringPainting:

                stateTimer -= Time.deltaTime;

                if (stateTimer <= 0f)
                {
                    DecideNextPainting();
                }

                break;


            case State.Patrolling:

                // Kept for compatibility with your original state.
                UpdateWalkBool(false);

                break;


            case State.Celebrating:

                UpdateWalkBool(false);

                break;
        }
    }

    // ============================================================
    // START
    // ============================================================

    public void BeginWalkingToEasel()
    {
        if (easelPoints == null || easelPoints.Length == 0)
        {
            Debug.LogError(
                "[HitlerNPC] No easelPoints assigned - can't start."
            );

            return;
        }

        if (debugLogging)
        {
            Debug.Log(
                $"[HitlerNPC] Starting painting routine. " +
                $"{easelPoints.Length} paintings available."
            );
        }

        currentPaintingIndex = -1;
        previousPaintingIndex = -1;
        nextInitialPaintingIndex = 0;
        completedInitialPaintingVisits = false;

        GoToNextPainting();
    }

    // ============================================================
    // CHOOSE NEXT PAINTING
    // ============================================================

    private void DecideNextPainting()
    {
        

        if (visitAllPaintingsFirst && !completedInitialPaintingVisits)
        {
            if (nextInitialPaintingIndex < easelPoints.Length)
            {
                int paintingToVisit = nextInitialPaintingIndex;

                nextInitialPaintingIndex++;

                if (nextInitialPaintingIndex >= easelPoints.Length)
                {
                    completedInitialPaintingVisits = true;
                }

                GoToPainting(paintingToVisit);

                return;
            }

            completedInitialPaintingVisits = true;
        }


        

        int randomPainting = GetRandomPaintingIndex();

        GoToPainting(randomPainting);
    }

    // ============================================================
    // RANDOM PAINTING SELECTION
    // ============================================================

    private int GetRandomPaintingIndex()
    {
        if (easelPoints.Length == 1)
            return 0;

        int randomIndex;

        do
        {
            randomIndex = Random.Range(
                0,
                easelPoints.Length
            );

        } while (
            preventSamePaintingTwice &&
            randomIndex == currentPaintingIndex
        );

        return randomIndex;
    }

    // ============================================================
    // GO TO PAINTING
    // ============================================================

    private void GoToNextPainting()
    {
        if (easelPoints == null || easelPoints.Length == 0)
            return;

        int nextPainting;

        if (!completedInitialPaintingVisits &&
            visitAllPaintingsFirst)
        {
            nextPainting = nextInitialPaintingIndex;

            nextInitialPaintingIndex++;

            if (nextInitialPaintingIndex >= easelPoints.Length)
            {
                completedInitialPaintingVisits = true;
            }
        }
        else
        {
            nextPainting = GetRandomPaintingIndex();
        }

        GoToPainting(nextPainting);
    }

    private void GoToPainting(int paintingIndex)
    {
        if (paintingIndex < 0 ||
            paintingIndex >= easelPoints.Length)
        {
            Debug.LogWarning(
                "[HitlerNPC] Invalid painting index."
            );

            return;
        }

        if (easelPoints[paintingIndex] == null)
        {
            Debug.LogWarning(
                $"[HitlerNPC] Painting {paintingIndex} has no Transform assigned."
            );

            return;
        }

        previousPaintingIndex = currentPaintingIndex;
        currentPaintingIndex = paintingIndex;

        hasCapturedPauseYaw = false;

        CurrentState = State.WalkingToEasel;

        agent.isStopped = false;

        agent.SetDestination(
            easelPoints[paintingIndex].position
        );

        if (debugLogging)
        {
            Debug.Log(
                $"[HitlerNPC] Walking to painting #{paintingIndex + 1}"
            );
        }
    }

    // ============================================================
    // ADMIRE PAINTING
    // ============================================================

    private void BeginAdmiring()
    {
        CurrentState = State.AdmiringPainting;

        agent.isStopped = true;

        UpdateWalkBool(false);

        // Face the painting.
        if (currentPaintingIndex >= 0 &&
            currentPaintingIndex < easelPoints.Length &&
            easelPoints[currentPaintingIndex] != null)
        {
            Vector3 lookPos =
                easelPoints[currentPaintingIndex].position;

            lookPos.y = transform.position.y;

            Vector3 direction =
                lookPos - transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );
            }
        }

        // Play smile animation if available.
        if (animator != null &&
            !string.IsNullOrEmpty(smileTrigger) &&
            animatorParams.Contains(smileTrigger))
        {
            animator.SetTrigger(smileTrigger);
        }

        // Admire the painting.
        stateTimer = admireDuration;

        // Add a random pause after admiring.
        stateTimer += Random.Range(
            paintingPauseRange.x,
            paintingPauseRange.y
        );

        if (debugLogging)
        {
            Debug.Log(
                $"[HitlerNPC] Admiring painting #{currentPaintingIndex + 1}"
            );
        }
    }

    // ============================================================
    // MOVEMENT FACING
    // ============================================================

    private void FaceMovementDirection()
    {
        Vector3 dir = agent.velocity;

        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                dir.normalized,
                Vector3.up
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    // ============================================================
    // ARRIVAL
    // ============================================================

    private bool HasArrived()
    {
        return
            !agent.pathPending &&
            agent.remainingDistance <= arriveDistance;
    }

    // ============================================================
    // WALK ANIMATION
    // ============================================================

    private void UpdateWalkBool(bool walking)
    {
        if (animator == null ||
            string.IsNullOrEmpty(walkBool))
        {
            return;
        }

        if (!animatorParams.Contains(walkBool))
            return;

        animator.SetBool(
            walkBool,
            walking
        );
    }

    // ============================================================
    // CELEBRATE
    // ============================================================

    public void Celebrate()
    {
        CurrentState = State.Celebrating;

        agent.isStopped = true;

        UpdateWalkBool(false);

       
        if (currentPaintingIndex >= 0 &&
            currentPaintingIndex < easelPoints.Length &&
            easelPoints[currentPaintingIndex] != null)
        {
            Vector3 lookPos =
                easelPoints[currentPaintingIndex].position;

            lookPos.y = transform.position.y;

            Vector3 direction =
                lookPos - transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up
                    );
            }
        }

        if (animator != null &&
            !string.IsNullOrEmpty(happyTrigger) &&
            animatorParams.Contains(happyTrigger))
        {
            animator.SetTrigger(happyTrigger);
        }
    }
}