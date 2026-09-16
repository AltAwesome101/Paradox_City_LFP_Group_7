using BetterEventBus;

public class AppleAirborneEvent: IGameplayEvent{
    public Apple Apple{get;}
    public AppleAirborneEvent(Apple apple) => Apple =apple;
}