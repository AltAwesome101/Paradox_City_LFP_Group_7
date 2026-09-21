using BetterSingletons;
using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
    public class ForestGameStateManager : Singleton<ForestGameStateManager>,
        IGamePlayEventListener<AppleCollectedEvent>,
        IGamePlayEventListener<LevelWonEvent>,
        IGamePlayEventListener<LevelLostEvent>
    {
        [SerializeField] int applesToWin = 10;
        int collected;

        void Start(){
            GameEventBus.Raise<ExplorationGameStateEvent>(new ExplorationGameStateEvent());
            Invoke(nameof(changeToPlayArea), 30f);
        }

        void changeToPlayArea(){
            GameEventBus.Raise<EnterPlayAreaEvent>(new EnterPlayAreaEvent());
        }
        void OnEnable()
        {
            GameEventBus.Register<AppleCollectedEvent>(this);
            GameEventBus.Register<LevelWonEvent>(this);
            GameEventBus.Register<LevelLostEvent>(this);
        }

        void OnDisable()
        {
            GameEventBus.Unregister<AppleCollectedEvent>(this);
            GameEventBus.Unregister<LevelWonEvent>(this);
            GameEventBus.Unregister<LevelLostEvent>(this);
        }

        public void OnGamePlayEvent(AppleCollectedEvent gameplayEvent)
        {
            collected++;
            if (collected >= applesToWin)
                GameEventBus.Raise(new LevelWonEvent());
        }

        public void OnGamePlayEvent(LevelWonEvent gameplayEvent)
        {
            // win UI / next-level trigger goes here
                Debug.Log("Game won!");
        }

        public void OnGamePlayEvent(LevelLostEvent gameplayEvent) => collected = 0;
    }

    // Order: [exploration] -> [Tutorial] -> [InGame]
    public class ExplorationGameStateEvent: IGameplayEvent{
        // 3D player movement
        // Open space
        // Free look/ follow camera
        static readonly Vector3 ExploreLocation = new Vector3(89f,.25f,200f); // Exploration position player teleports to when the exploration state

        public ExplorationGameStateEvent(){
            GameEventBus.Raise<CameraChangeEvent>(new CameraChangeEvent(Forestlevel.CameraType.FreeLook)); //Camera Raise event
            GameEventBus.Raise<IMovementStrategy>(new TraversalMovement()); //Player movement 3d raise event;
            GameEventBus.Raise<PlayerLocationEvent>(new PlayerLocationEvent{Destination = ExploreLocation}); // player Location change event
            Debug.Log($"Player's new position:{ExploreLocation}");
        }
    }

    public class TutorialGameStateEvent: IGameplayEvent{}

    public class EnterPlayAreaEvent: IGameplayEvent
    {
        // idle movement
        // Fixed space meaning tp player
        // Fixed camera view
        static readonly Vector3 fixedLocation = new Vector3(-0.2f,1.077f,57.9f); // fixed position player teleports to when the tutorial state

        public EnterPlayAreaEvent(){
            GameEventBus.Raise<CameraChangeEvent>(new CameraChangeEvent(Forestlevel.CameraType.InGame)); //Camera Raise event
            GameEventBus.Raise<IMovementStrategy>(new IdleMovement()); //Player movement 3d raise event;
            GameEventBus.Raise<PlayerLocationEvent>(new PlayerLocationEvent{Destination = fixedLocation}); // player Location change event            
            Debug.Log($"Player's new position:{fixedLocation}");

        }
    }


    public class InGameGameStateEvent: IGameplayEvent
    {
        // Raised after TutorialGameStateEvent 
        // Player movement = in game 2D lateral movement
        public InGameGameStateEvent(){
            GameEventBus.Raise<IMovementStrategy>(new InGameMovement()); //Player movement 2D raise event;
        }
    }
}

