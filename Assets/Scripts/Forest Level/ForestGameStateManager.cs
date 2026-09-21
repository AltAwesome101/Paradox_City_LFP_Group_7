using BetterSingletons;
using BetterEventBus;
using UnityEngine;
using Forestlevel;

public class ForestGameStateManager : Singleton<ForestGameStateManager>,
    IGamePlayEventListener<AppleCollectedEvent>,
    IGamePlayEventListener<LevelWonEvent>,
    IGamePlayEventListener<LevelLostEvent>
{
    [SerializeField] int applesToWin = 10;
    int collected;

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

    public void OnGamePlayEvent(LevelLostEvent gameplayEvent)
    {
        // restart the level here
        collected = gameplayEvent.collect; //reset score to 0

    }
}

// Order: [exploration] -> [Tutorial] -> [InGame]
public class ExplorationGameStateEvent: IGameplayEvent{
    // 3D player movement
    // Open space
    // Free look/ follow camera

    public ExplorationGameStateEvent(){
        GameEventBus.Raise<CameraChangeEvent>(new CameraChangeEvent(Forestlevel.CameraType.FreeLook)); //Camera Raise event
        GameEventBus.Raise<IMovementStrategy>(new TraversalMovement()); //Player movement 3d raise event;
    }
}

public class TutorialGameStateEvent: IGameplayEvent{
    // idle movement
    // Fixed space meaning tp player
    // Fixed camera view
    readonly Vector3 fixedLocation = new Vector3(-0.2f,1.077f,57.9f); // fixed position player teleports to when the tutorial state
    public TutorialGameStateEvent(){
        GameEventBus.Raise<CameraChangeEvent>(new CameraChangeEvent(Forestlevel.CameraType.InGame)); //Camera Raise event
        GameEventBus.Raise<IMovementStrategy>(new IdleMovement()); //Player movement 3d raise event;
        GameEventBus.Raise<PlayerLocationEvent>(new PlayerLocationEvent{Destination = fixedLocation}); // player Location change event
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
