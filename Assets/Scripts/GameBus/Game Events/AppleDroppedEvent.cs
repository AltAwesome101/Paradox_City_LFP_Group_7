using BetterEventBus;
using UnityEngine;

public class AppleDroppedEvent : IGameplayEvent
{
    public Apple Apple{get;}
    public AppleDroppedEvent(Apple apple) {
        Apple = apple;
    }
}


public class LevelWonEvent:IGameplayEvent{}
public class LevelLostEvent : IGameplayEvent{}