using System.Collections.Generic;
using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
   
    public class PlayerCatchPose : MonoBehaviour,
        IGamePlayEventListener<AppleWarningStartedEvent>,
        IGamePlayEventListener<AppleWarningEndedEvent>,
        IGamePlayEventListener<AppleCollectedEvent>,
        IGamePlayEventListener<LevelWonEvent>,
        IGamePlayEventListener<LevelLostEvent>
    {
        [SerializeField] Animator animator;
        [SerializeField] string handsUpParameter = "HandsUp";

        [Header("Catch zone (distance from the apple deployer)")]
        [Tooltip("Hands go up when the player gets this close to a pending apple.")]
        [SerializeField] float enterRadius = 1.5f;
        [Tooltip("Hands only come down once the player is further than this (prevents flicker).")]
        [SerializeField] float exitRadius = 2f;
        [Tooltip("Ignore depth and only compare X. Use this if your deployers sit at a different Z than the player.")]
        [SerializeField] bool useXAxisOnly;

        [Header("Timing")]
        [Tooltip("How long hands stay up after the apple is released if it isn't caught.")]
        [SerializeField] float holdAfterRelease = 1.5f;

        class PendingApple
        {
            public Transform source;
            public bool released;
            public float expiresAt;
        }

        readonly List<PendingApple> pending = new();
        int handsUpHash;
        bool handsUp;

        void Awake()
        {
            if (!animator) animator = GetComponentInChildren<Animator>();
            handsUpHash = Animator.StringToHash(handsUpParameter);
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
            ClearAll();
        }

        void Update()
        {
            
            for (int i = pending.Count - 1; i >= 0; i--)
                if (pending[i].released && Time.time >= pending[i].expiresAt)
                    pending.RemoveAt(i);

            SetHandsUp(IsUnderAnyPendingApple());
        }

        bool IsUnderAnyPendingApple()
        {
            float radius = handsUp ? exitRadius : enterRadius;
            foreach (var p in pending)
                if (p.source && HorizontalDistance(p.source.position) <= radius)
                    return true;
            return false;
        }

        float HorizontalDistance(Vector3 target)
        {
            Vector3 d = target - transform.position;
            return useXAxisOnly ? Mathf.Abs(d.x) : new Vector2(d.x, d.z).magnitude;
        }

        void SetHandsUp(bool value)
        {
            if (value == handsUp) return;
            handsUp = value;
            if (animator) animator.SetBool(handsUpHash, value);
        }

        void ClearAll()
        {
            pending.Clear();
            SetHandsUp(false);
        }

        // ---- events ----

        public void OnGamePlayEvent(AppleWarningStartedEvent e)
        {
            pending.Add(new PendingApple { source = e.Source });
        }

        public void OnGamePlayEvent(AppleWarningEndedEvent e)
        {
            foreach (var p in pending)
            {
                if (p.source == e.Source && !p.released)
                {
                    p.released = true;
                    p.expiresAt = Time.time + holdAfterRelease;
                    break;
                }
            }
        }

        
        public void OnGamePlayEvent(AppleCollectedEvent e)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < pending.Count; i++)
            {
                if (!pending[i].released || !pending[i].source) continue;
                float d = HorizontalDistance(pending[i].source.position);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            if (best >= 0) pending.RemoveAt(best);
        }

        public void OnGamePlayEvent(LevelWonEvent e) => ClearAll();
        public void OnGamePlayEvent(LevelLostEvent e) => ClearAll();

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, enterRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, exitRadius);
        }
    }

    
}