using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
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
        Celebrating,
        Listening,
        Investigating
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

    [Header("Player Awareness - Hearing")]
    [Tooltip("Assign the player. Leave empty to auto-find the GameObject tagged 'Player'.")]
    public Transform player;

    [Tooltip("How far away Hitler can hear the player's footsteps.")]
    public float hearingRadius = 8f;

    [Tooltip("The player must be moving at least this fast (units/second) to make noise Hitler can hear. Stops standing still nearby from counting as footsteps.")]
    public float minPlayerSpeedToMakeNoise = 0.6f;

    [Tooltip("If enabled, a wall between Hitler and the player (on the mask below) fully muffles footsteps.")]
    public bool wallsBlockHearing = false;

    [Tooltip("Layers treated as walls for the hearing check above.")]
    public LayerMask hearingBlockMask;

    [Tooltip("Minimum seconds between noise reactions, so rapid footsteps don't spam him with new reactions.")]
    public float noiseReactionCooldown = 1.5f;

    [Header("Player Awareness - Vision")]
    [Tooltip("How far Hitler can see the player.")]
    public float visionRange = 12f;

    [Tooltip("Full width of Hitler's forward field of view, in degrees.")]
    [Range(0f, 360f)]
    public float visionAngle = 100f;

    [Tooltip("Layers that block line of sight (walls, furniture, etc). Leave as Nothing to skip the occlusion check.")]
    public LayerMask visionBlockMask;

    [Header("Reactions")]
    [Tooltip("How long Hitler keeps glancing toward a sound before giving up on it, if he never actually spots the player.")]
    public float listenDuration = 2.5f;

    [Tooltip("If a noise or sighting happens within this distance, Hitler walks over to investigate instead of just turning his head.")]
    public float investigateDistance = 4f;

    [Tooltip("Maximum seconds Hitler will spend travelling toward a noise before giving up on reaching it.")]
    public float investigateTravelTimeout = 6f;

    [Tooltip("Seconds Hitler spends looking around once he reaches the last known position, before resuming his routine.")]
    public float investigateLookDuration = 3f;

    [Header("Optional Animation")]
    [Tooltip("Leave empty if you haven't set up animations yet.")]
    public Animator animator;

    public string smileTrigger = "Smile";
    public string happyTrigger = "Happy";
    public string alertTrigger = "Alert";
    public string walkBool = "IsWalking";

    [Header("Events")]
    [Tooltip("Fired the moment Hitler directly spots the player.")]
    public UnityEvent onPlayerSpotted;

    [Tooltip("Fired when Hitler hears footsteps but hasn't seen the player yet.")]
    public UnityEvent onPlayerHeard;

    [Tooltip("Fired when Hitler gives up looking and resumes his normal routine.")]
    public UnityEvent onLostPlayer;

    [Header("Debug")]
    public bool debugLogging = true;

    public State CurrentState { get; private set; } = State.Idle;

    /// <summary>True while Hitler is actively reacting to the player (Listening or Investigating).</summary>
    public bool IsAwareOfPlayer => CurrentState == State.Listening || CurrentState == State.Investigating;

    /// <summary>True on frames where Hitler has a direct, unobstructed line of sight to the player.</summary>
    public bool CanCurrentlySeePlayer => playerCurrentlyVisible;

    private NavMeshAgent agent;

    private float stateTimer = 0f;

    private int currentPaintingIndex = -1;

    private int previousPaintingIndex = -1;

    private int nextInitialPaintingIndex = 0;

    private bool completedInitialPaintingVisits = false;

    private bool hasCapturedPauseYaw;
    private float pausedBaseYaw;

    private HashSet<string> animatorParams;

    // Player awareness state
    private State stateBeforeReaction = State.Idle;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 lastPlayerPosition;
    private float playerSpeedEstimate;
    private bool playerCurrentlyVisible;
    private float reactionTimer;
    private float lastNoiseReactionTime = -999f;
    private bool investigateArrived;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;

        CacheAnimatorParameters();

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");

            if (p != null)
                player = p.transform;
            else if (debugLogging)
                Debug.LogWarning(
                    "[HitlerNPC] No player assigned and no GameObject tagged 'Player' was found - " +
                    "hearing and vision will be disabled."
                );
        }

        if (player != null)
            lastPlayerPosition = player.position;

        // So he's not a frozen statue before his painting routine has even started.
        pausedBaseYaw = transform.eulerAngles.y;
        hasCapturedPauseYaw = true;
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
        WarnIfMissing(alertTrigger, "Trigger");
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
        UpdatePlayerAwareness();

        switch (CurrentState)
        {
            case State.Idle:

                ApplyLookAroundSweep();

                UpdateWalkBool(false);

                break;


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

                ApplyLookAroundSweep();

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


            case State.Listening:

                HandleListening();

                break;


            case State.Investigating:

                HandleInvestigating();

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

        // Remember this heading so the look-around sweep has a base to swing from.
        pausedBaseYaw = transform.eulerAngles.y;
        hasCapturedPauseYaw = true;

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
    // LOOK-AROUND SWEEP
    // ============================================================

    /*
     * Gently swings Hitler's heading side to side around whatever base
     * yaw was last captured (his admiring angle, or his starting
     * rotation while idle). Purely cosmetic - makes him feel alive
     * instead of a frozen statue between actions.
     */
    private void ApplyLookAroundSweep()
    {
        if (!hasCapturedPauseYaw)
            return;

        float sweep =
            Mathf.Sin(Time.time * lookAroundSpeed) *
            lookAroundAngle;

        Quaternion target =
            Quaternion.Euler(0f, pausedBaseYaw + sweep, 0f);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                target,
                turnSpeed * Time.deltaTime
            );
    }

    // ============================================================
    // PLAYER AWARENESS
    // ============================================================

    private void UpdatePlayerAwareness()
    {
        if (player == null)
            return;

        float measuredSpeed =
            (player.position - lastPlayerPosition).magnitude /
            Mathf.Max(Time.deltaTime, 0.0001f);

        playerSpeedEstimate =
            Mathf.Lerp(playerSpeedEstimate, measuredSpeed, 0.5f);

        lastPlayerPosition = player.position;

        playerCurrentlyVisible = CanSeePlayer();

        // Don't interrupt his victory moment.
        if (CurrentState == State.Celebrating)
            return;

        bool alreadyReacting =
            CurrentState == State.Listening ||
            CurrentState == State.Investigating;

        if (playerCurrentlyVisible)
        {
            lastKnownPlayerPosition = player.position;

            // If he's merely listening (hasn't committed to moving yet) and
            // then actually spots them, that always escalates to a full
            // investigation - just glancing over isn't enough anymore.
            bool needsEscalation = !alreadyReacting || CurrentState == State.Listening;

            if (needsEscalation)
            {
                ReactToPlayer(seen: true);
            }
            else
            {
                // Already investigating and can see them - keep his interest
                // topped up and keep chasing a moving target rather than an
                // old position.
                reactionTimer = Mathf.Max(reactionTimer, 0.5f);

                if (!investigateArrived &&
                    Vector3.Distance(agent.destination, lastKnownPlayerPosition) > 0.5f)
                {
                    agent.SetDestination(lastKnownPlayerPosition);
                }
            }

            return;
        }

        if (alreadyReacting)
            return;

        if (Time.time - lastNoiseReactionTime < noiseReactionCooldown)
            return;

        float distanceToPlayer =
            Vector3.Distance(transform.position, player.position);

        bool inHearingRange = distanceToPlayer <= hearingRadius;
        bool loudEnough = playerSpeedEstimate >= minPlayerSpeedToMakeNoise;
        bool notMuffled = !wallsBlockHearing || HasHearingLineOfSight();

        if (inHearingRange && loudEnough && notMuffled)
        {
            lastNoiseReactionTime = Time.time;
            lastKnownPlayerPosition = player.position;

            ReactToPlayer(seen: false);
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 toPlayer = player.position - transform.position;

        if (toPlayer.magnitude > visionRange)
            return false;

        Vector3 flatToPlayer = toPlayer;
        flatToPlayer.y = 0f;

        if (flatToPlayer.sqrMagnitude > 0.0001f)
        {
            float angle = Vector3.Angle(transform.forward, flatToPlayer);

            if (angle > visionAngle * 0.5f)
                return false;
        }

        if (visionBlockMask.value != 0)
        {
            Vector3 eyeOrigin = transform.position + Vector3.up * 1.6f;
            Vector3 targetPoint = player.position + Vector3.up * 1f;

            if (Physics.Linecast(eyeOrigin, targetPoint, visionBlockMask, QueryTriggerInteraction.Ignore))
                return false;
        }

        return true;
    }

    private bool HasHearingLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 target = player.position + Vector3.up * 1f;

        return !Physics.Linecast(origin, target, hearingBlockMask, QueryTriggerInteraction.Ignore);
    }

    private void ReactToPlayer(bool seen)
    {
        if (CurrentState != State.Listening &&
            CurrentState != State.Investigating)
        {
            stateBeforeReaction = CurrentState;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool shouldInvestigate = seen || distance <= investigateDistance;

        if (shouldInvestigate)
        {
            CurrentState = State.Investigating;
            investigateArrived = false;

            agent.isStopped = false;
            agent.SetDestination(lastKnownPlayerPosition);

            reactionTimer = investigateTravelTimeout;
        }
        else
        {
            CurrentState = State.Listening;

            agent.isStopped = true;

            reactionTimer = listenDuration;
        }

        if (animator != null &&
            !string.IsNullOrEmpty(alertTrigger) &&
            animatorParams.Contains(alertTrigger))
        {
            animator.SetTrigger(alertTrigger);
        }

        if (debugLogging)
        {
            Debug.Log(
                $"[HitlerNPC] {(seen ? "Spotted" : "Heard")} the player - now {CurrentState}."
            );
        }

        if (seen)
            onPlayerSpotted?.Invoke();
        else
            onPlayerHeard?.Invoke();
    }

    private void HandleListening()
    {
        UpdateWalkBool(false);

        Vector3 dir = lastKnownPlayerPosition - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(dir.normalized, Vector3.up);

            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
        }

        reactionTimer -= Time.deltaTime;

        if (reactionTimer <= 0f)
        {
            EndReaction();
        }
    }

    private void HandleInvestigating()
    {
        if (!investigateArrived)
        {
            FaceMovementDirection();

            UpdateWalkBool(agent.velocity.sqrMagnitude > 0.05f);

            bool arrived =
                !agent.pathPending &&
                agent.remainingDistance <= arriveDistance;

            reactionTimer -= Time.deltaTime;

            if (arrived)
            {
                investigateArrived = true;

                agent.isStopped = true;

                reactionTimer = investigateLookDuration;
            }
            else if (reactionTimer <= 0f)
            {
                // Took too long to get there - give up the chase.
                EndReaction();
            }

            return;
        }

        UpdateWalkBool(false);

        // Slow visual sweep around the spot he last knew about, while
        // deciding whether to give up and resume his routine.
        float sweep =
            Mathf.Sin(Time.time * lookAroundSpeed) *
            lookAroundAngle;

        Vector3 toLastKnown = lastKnownPlayerPosition - transform.position;
        toLastKnown.y = 0f;

        Quaternion baseRotation =
            toLastKnown.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(toLastKnown.normalized, Vector3.up)
                : transform.rotation;

        Quaternion sweptRotation =
            baseRotation * Quaternion.Euler(0f, sweep, 0f);

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                sweptRotation,
                turnSpeed * Time.deltaTime
            );

        reactionTimer -= Time.deltaTime;

        if (reactionTimer <= 0f)
        {
            EndReaction();
        }
    }

    private void EndReaction()
    {
        if (debugLogging)
        {
            Debug.Log(
                "[HitlerNPC] Lost track of the player - resuming his routine."
            );
        }

        onLostPlayer?.Invoke();

        CurrentState = stateBeforeReaction;

        if (CurrentState == State.WalkingToEasel &&
            currentPaintingIndex >= 0 &&
            currentPaintingIndex < easelPoints.Length &&
            easelPoints[currentPaintingIndex] != null)
        {
            agent.isStopped = false;

            agent.SetDestination(
                easelPoints[currentPaintingIndex].position
            );
        }
        else
        {
            // AdmiringPainting, Idle, Patrolling: nothing needs to move,
            // and his remaining stateTimer (if any) simply keeps ticking.
            agent.isStopped = true;
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

    // ============================================================
    // GIZMOS
    // ============================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        // Hearing radius.
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        // Vision range.
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Vision cone edges.
        Vector3 forward = transform.forward;

        Quaternion leftRot = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f);
        Quaternion rightRot = Quaternion.Euler(0f, visionAngle * 0.5f, 0f);

        Gizmos.DrawLine(transform.position, transform.position + leftRot * forward * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightRot * forward * visionRange);
    }

#endif
}