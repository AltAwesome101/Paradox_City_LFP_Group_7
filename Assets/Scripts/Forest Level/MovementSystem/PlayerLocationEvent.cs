using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
    public struct PlayerLocationEvent : IGameplayEvent
    {
        public Vector3 Destination{get;set;}
    }
}