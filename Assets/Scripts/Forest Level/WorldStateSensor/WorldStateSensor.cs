using System;
using System.Collections.Generic;
using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
    /// <summary>
    /// Aggregates "what does the player currently perceive": nearby apples, ledge/obstacle
    /// ahead. Each fact is a query method plus a Changed event (same shape as GOAP's
    /// Sensor.OnTargetChanged), so anything - PlayerCatchPose, a future GOAP agent,
    /// parkour - reads one source of truth instead of re-deriving it.
    ///
    /// Apples are tracked by event, not a trigger collider: "near" is a caller-chosen
    /// radius (see PlayerCatchPose's enter/exit hysteresis), which a single fixed-radius
    /// SphereCollider can't represent. Ledge/obstacle checks are the part that benefits
    /// from the base Sensor's timer-throttle, since raycasts are direction-dependent and
    /// don't need to run every frame.
    /// </summary>
    public class WorldStateSensor : MonoBehaviour,
        IGamePlayEventListener<AppleWarningStartedEvent>,
        IGamePlayEventListener<AppleWarningEndedEvent>,
        IGamePlayEventListener<AppleCollectedEvent>,
        IGamePlayEventListener<LevelWonEvent>,
        IGamePlayEventListener<LevelLostEvent>
    {
        [Header("Apples")]
        [Tooltip("Ignore depth and only compare X. Use this if your deployers sit at a different Z than the player.")]
        [SerializeField] bool useXAxisOnly;
        [Tooltip("How long an apple stays tracked after release if it isn't caught.")]
        [SerializeField] float appleHoldAfterRelease = 1.5f;

        [Header("Ground / ledges")]
        [SerializeField] EnvironmentChecker environmentChecker;
        [Tooltip("How often ledge/obstacle checks re-run. 0 = every frame.")]
        [SerializeField] float groundCheckInterval = 0.1f;

        [Header("Gizmo")]
        [SerializeField] Color defaultColour = Color.green;
        [SerializeField] Color changedColour = Color.red;

        // ---- apples (event-driven) ----
        class PendingApple
        {
            public Transform source;
            public bool released;
            public float expiresAt;
        }
        readonly List<PendingApple> pendingApples = new();

        public int PendingAppleCount => pendingApples.Count;
        public event Action OnAppleStateChanged = delegate { };

        // ---- ground (timer-polled) ----
        Timer groundTimer;
        public bool ObstacleAhead { get; private set; }
        public ObstacleInfo ObstacleInfo { get; private set; }
        public bool LedgeAhead { get; private set; }
        public LedgeInfo LedgeInfo { get; private set; }
        public event Action OnGroundStateChanged = delegate { };

        void Awake()
        {
            if (!environmentChecker) environmentChecker = GetComponent<EnvironmentChecker>();
        }

        void Start()
        {
            // ASSUMPTION: Timer / CountdownTimer come from the same utility namespace as
            // your GOAP Sensor - adjust the using if they live elsewhere.
            if (groundCheckInterval > 0f)
            {
                groundTimer = new CountdownTimer(groundCheckInterval);
                groundTimer.OnTimerStop += () =>
                {
                    RefreshGroundState(transform.forward);
                    groundTimer.Start();
                };
                groundTimer.Start();
            }
        }

        void OnEnable()
        {
            GameEventBus.Register<AppleWarningStartedEvent>(this);
            GameEventBus.Register<AppleWarningEndedEvent>(this);
            GameEventBus.Register<AppleCollectedEvent>(this);
            GameEventBus.Register<LevelWonEvent>(this);
            GameEventBus.Register<LevelLostEvent>(this);
        }

        void OnDisable()
        {
            GameEventBus.Unregister<AppleWarningStartedEvent>(this);
            GameEventBus.Unregister<AppleWarningEndedEvent>(this);
            GameEventBus.Unregister<AppleCollectedEvent>(this);
            GameEventBus.Unregister<LevelWonEvent>(this);
            GameEventBus.Unregister<LevelLostEvent>(this);
            pendingApples.Clear();
        }

        void Update()
        {
            bool hadPending = pendingApples.Count > 0;
            for (int i = pendingApples.Count - 1; i >= 0; i--)
                if (pendingApples[i].released && Time.time >= pendingApples[i].expiresAt)
                    pendingApples.RemoveAt(i);
            if (hadPending && pendingApples.Count == 0)
                OnAppleStateChanged.Invoke();

            if (groundCheckInterval <= 0f)
                RefreshGroundState(transform.forward);
            else
                groundTimer?.Tick(Time.deltaTime);
        }

        // ---- public queries ----

        /// <summary>True if any tracked apple's source is within radius (horizontal distance).</summary>
        public bool IsAppleNear(float radius)
        {
            foreach (var p in pendingApples)
                if (p.source && HorizontalDistance(p.source.position) <= radius)
                    return true;
            return false;
        }

        float HorizontalDistance(Vector3 target)
        {
            Vector3 d = target - transform.position;
            return useXAxisOnly ? Mathf.Abs(d.x) : new Vector2(d.x, d.z).magnitude;
        }

        void RefreshGroundState(Vector3 moveDirection)
        {
            var obstacle = environmentChecker.CheckObstacle();
            bool obstacleChanged = obstacle.hitFound != ObstacleAhead;
            ObstacleAhead = obstacle.hitFound;
            ObstacleInfo = obstacle;

            bool ledgeFound = environmentChecker.CheckLedge(moveDirection, out var ledge);
            bool ledgeChanged = ledgeFound != LedgeAhead;
            LedgeAhead = ledgeFound;
            LedgeInfo = ledge;

            if (obstacleChanged || ledgeChanged)
                OnGroundStateChanged.Invoke();
        }

        // ---- apple events ----

        public void OnGamePlayEvent(AppleWarningStartedEvent e)
        {
            pendingApples.Add(new PendingApple { source = e.Source });
            OnAppleStateChanged.Invoke();
        }

        public void OnGamePlayEvent(AppleWarningEndedEvent e)
        {
            foreach (var p in pendingApples)
            {
                if (p.source == e.Source && !p.released)
                {
                    p.released = true;
                    p.expiresAt = Time.time + appleHoldAfterRelease;
                    break;
                }
            }
        }

        // The caught apple is the released one closest to the player.
        public void OnGamePlayEvent(AppleCollectedEvent e)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < pendingApples.Count; i++)
            {
                if (!pendingApples[i].released || !pendingApples[i].source) continue;
                float d = HorizontalDistance(pendingApples[i].source.position);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            if (best >= 0)
            {
                pendingApples.RemoveAt(best);
                OnAppleStateChanged.Invoke();
            }
        }

        public void OnGamePlayEvent(LevelWonEvent e) => ClearApples();
        public void OnGamePlayEvent(LevelLostEvent e) => ClearApples();

        void ClearApples()
        {
            if (pendingApples.Count == 0) return;
            pendingApples.Clear();
            OnAppleStateChanged.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = pendingApples.Count > 0 ? changedColour : defaultColour;
            // No single radius to draw here - IsAppleNear takes an arbitrary radius per caller.
        }
    }
}