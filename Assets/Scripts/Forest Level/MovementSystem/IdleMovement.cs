using UnityEngine;

namespace Forestlevel
{
    public class IdleMovement : IMovementStrategy
    {
        IMovementOwner owner;

        public void OnEnter(IMovementOwner owner)
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