using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
    // Raised by AppleDeployManager when a deployer starts its warning countdown
    // (the apple is about to fall). PlayerCatchPose listens for this to start
    // tracking the deployer.
    public class AppleWarningStartedEvent : IGameplayEvent
    {
        public readonly Transform Source;
        public AppleWarningStartedEvent(Transform source) => Source = source;
    }

    // Raised by AppleDeployManager the moment the countdown ends and the apple is
    // released. PlayerCatchPose keeps hands up until it's caught or times out.
    public class AppleWarningEndedEvent : IGameplayEvent
    {
        public readonly Transform Source;
        public AppleWarningEndedEvent(Transform source) => Source = source;
    }
}