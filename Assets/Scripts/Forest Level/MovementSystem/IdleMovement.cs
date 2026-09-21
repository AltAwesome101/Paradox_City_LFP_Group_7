using UnityEngine;

namespace Forestlevel
{
    public class IdleMovement : IMovementStrategy
    {
        PlayerMovement owner;

        public void OnEnter(PlayerMovement owner)
        {
            this.owner = owner;
        }

        public void OnExit()
        {
            owner = null;
        }

        public Vector3 GetHorizontalTarget(in MovementContext ctx)
        {
            return Vector3.zero;
        }

    }

}

