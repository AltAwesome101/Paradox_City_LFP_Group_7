using BetterSingletons;
using BetterEventBus;
using UnityEngine;

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
        if (collected >= applesToWin) GameEventBus.Raise(new LevelWonEvent());
    }

    public void OnGamePlayEvent(LevelWonEvent gameplayEvent)
    {
        // win UI / next-level trigger goes here
    }

    public void OnGamePlayEvent(LevelLostEvent gameplayEvent)
    {
        // restart the level here
    }
}
