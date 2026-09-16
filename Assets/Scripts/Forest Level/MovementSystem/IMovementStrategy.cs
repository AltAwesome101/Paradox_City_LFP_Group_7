using UnityEngine;

namespace Forestlevel
{
    public interface IMovementStrategy
    {
        void OnEnter(PlayerMovement owner);
        void OnExit();
        Vector3 GetHorizontalTarget(in MovementContext ctx);
    }

}

