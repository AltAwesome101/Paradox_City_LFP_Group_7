using BetterEventBus;
using BetterStateMachine;
using UnityEngine;

namespace Forestlevel
{
    public class PlayerController : EntityController, IMovementOwner,
    IGamePlayEventListener<IMovementStrategy>,
    IGamePlayEventListener<PlayerLocationEvent>
    {
        [Header("InputReader")]
        [SerializeField] InputReaderSO input;
        [Header("Sensor")]
        [SerializeField] WorldStateSensor sensor;

        // Camera Declaration
        Transform cameraTransform = Camera.main.transform;
        public Transform CameraTransform => cameraTransform;

        //Animation Declaration
        [Header("Animation")]
        [SerializeField] AnimatorController animatorController;
        public AnimatorController AnimatorController => animatorController;

        // Motion Declaration
        [Header("Motion")]
        [SerializeField] PlayerMotion playerMotion;
        IMovementStrategy movementStrategy;
        public IMovementStrategy MovementStrategy => movementStrategy;

        // StateMachine Declaration
        StateMachine machine = new();


        void OnEnable(){
            GameEventBus.Register<IMovementStrategy>(this);
            GameEventBus.Register<PlayerLocationEvent>(this);
        }
        void OnDisable(){
            GameEventBus.Unregister<IMovementStrategy>(this);
            GameEventBus.Unregister<PlayerLocationEvent>(this);
        }

        void Update() => machine?.Update();
        void FixedUpdate() => machine?.FixedUpdate();

        void BuildStateMachine(){
            // states
            var locked = new LockedState(this,motion: playerMotion, anim: animatorController);

            // transitions

        }

        public void OnGamePlayEvent(IMovementStrategy movementStrategyevt)
        {
            throw new System.NotImplementedException();
        }

        public void OnGamePlayEvent(PlayerLocationEvent gameplayEvent)
        {
            throw new System.NotImplementedException();
        }
    }
    
}
