using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
    public struct PlayerLocationEvent : IGameplayEvent
    {
        public readonly Vector3 Destination{get;}
        public PlayerLocationEvent(Vector3 destination){
            Destination = destination;
        }
    }
}