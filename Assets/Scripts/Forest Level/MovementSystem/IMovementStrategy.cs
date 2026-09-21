using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
    public interface IMovementStrategy : IGameplayEvent
    {
        void OnEnter(PlayerMovement owner);
        void OnExit();
        Vector3 GetHorizontalTarget(in MovementContext ctx);
    }
}

