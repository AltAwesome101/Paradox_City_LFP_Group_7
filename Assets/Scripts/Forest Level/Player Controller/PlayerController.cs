using BetterEventBus;
using BetterStateMachine;
using UnityEngine;

namespace Forestlevel
{
    public class PlayerController : EntityController, IMovementOwner,
        IGamePlayEventListener<IMovementStrategy>,
        IGamePlayEventListener<ExplorationGameStateEvent>,
        IGamePlayEventListener<TutorialGameStateEvent>,
        IGamePlayEventListener<InGameGameStateEvent>,
        IGamePlayEventListener<LevelWonEvent>,
        IGamePlayEventListener<LevelLostEvent>,
        IGamePlayEventListener<PlayerLocationEvent>
    {
        [Header("InputReader")]
        [SerializeField] InputReaderSO input;

        [Header("Sensor")]
        [SerializeField] WorldStateSensor sensor;

        [Header("Apple catch pose")]
        [Tooltip("Hands go up when the player gets this close to a pending apple.")]
        [SerializeField] float catchEnterRadius = 1.5f;
        [Tooltip("Hands only come down once the player is further than this (prevents flicker).")]
        [SerializeField] float catchExitRadius = 2f;

        [Header("Animation")]
        [SerializeField] AnimatorController animatorController;
        public AnimatorController AnimatorController => animatorController;

        [Header("Motion")]
        [SerializeField] PlayerMotion playerMotion;

        // Camera - resolved lazily; Camera.main as a field initializer runs before the
        // scene is guaranteed ready.
        Transform cameraTransform;
        public Transform CameraTransform => cameraTransform;

        // IMovementOwner: single source of truth for the live strategy. States raise
        // changes through the bus instead of setting this directly, so parkour and
        // states go through the same path.
        IMovementStrategy movementStrategy;
        public IMovementStrategy MovementStrategy => movementStrategy;

        StateMachine machine = new();
        GamePhase phase = GamePhase.Exploration;

        void Awake()
        {
            if (!playerMotion) playerMotion = GetComponent<PlayerMotion>();
            if (!animatorController) animatorController = GetComponent<AnimatorController>();
            if (!sensor) sensor = GetComponent<WorldStateSensor>();

            cameraTransform = Camera.main ? Camera.main.transform : null;

            BuildStateMachine();
        }

        void OnEnable()
        {
            GameEventBus.Register<IMovementStrategy>(this);
            GameEventBus.Register<ExplorationGameStateEvent>(this);
            GameEventBus.Register<TutorialGameStateEvent>(this);
            GameEventBus.Register<InGameGameStateEvent>(this);
            GameEventBus.Register<LevelWonEvent>(this);
            GameEventBus.Register<LevelLostEvent>(this);
            GameEventBus.Register<PlayerLocationEvent>(this);
        }

        void OnDisable()
        {
            GameEventBus.Unregister<IMovementStrategy>(this);
            GameEventBus.Unregister<ExplorationGameStateEvent>(this);
            GameEventBus.Unregister<TutorialGameStateEvent>(this);
            GameEventBus.Unregister<InGameGameStateEvent>(this);
            GameEventBus.Unregister<LevelWonEvent>(this);
            GameEventBus.Unregister<LevelLostEvent>(this);
            GameEventBus.Unregister<PlayerLocationEvent>(this);
        }

        void Update() => machine?.Update();
        void FixedUpdate() => machine?.FixedUpdate();

        void BuildStateMachine()
        {
            var freeRoam = new FreeRoamState(this, playerMotion, animatorController, input, sensor, this);
            var appleCatching = new AppleCatchingState(this, playerMotion, animatorController, input, sensor, this, catchEnterRadius, catchExitRadius);
            var locked = new LockedState(this, playerMotion, animatorController);

            // Tutorial routes to Locked, not FreeRoam: Tutorial's actual requirement is
            // "no movement, cursor free for UI", which is exactly what LockedState already
            // does - no separate idle-strategy object needed. FreeRoam now means
            // Exploration specifically, and owns its own default strategy again (see
            // FreeRoamState.OnEnter) since the game-state events no longer raise one.
            machine.AddAnyTransition(freeRoam, new FuncPredicate(() => phase == GamePhase.Exploration));
            machine.AddAnyTransition(appleCatching, new FuncPredicate(() => phase == GamePhase.InGame));
            machine.AddAnyTransition(locked, new FuncPredicate(() => phase == GamePhase.Tutorial || phase == GamePhase.Won || phase == GamePhase.Lost));

            machine.SetState(freeRoam);
        }

        public void OnGamePlayEvent(IMovementStrategy e)
        {
            if (e == null) return;
            movementStrategy?.OnExit();
            movementStrategy = e;
            movementStrategy.OnEnter(this);
        }

        public void OnGamePlayEvent(ExplorationGameStateEvent e) => phase = GamePhase.Exploration;
        public void OnGamePlayEvent(TutorialGameStateEvent e) => phase = GamePhase.Tutorial;
        public void OnGamePlayEvent(InGameGameStateEvent e) => phase = GamePhase.InGame;
        public void OnGamePlayEvent(LevelWonEvent e) => phase = GamePhase.Won;
        public void OnGamePlayEvent(LevelLostEvent e) => phase = GamePhase.Lost;

        public void OnGamePlayEvent(PlayerLocationEvent e) => playerMotion.Teleport(e.Destination);

        // GamePhase is declared in ForestGameStateManager.cs (same namespace) - it's the
        // level-flow source of truth, this controller just reads it.
    }
}

public enum GamePhase
{
    Exploration,
    Tutorial,
    InGame,
    Won,
    Lost
}