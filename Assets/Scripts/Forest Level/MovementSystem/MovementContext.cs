using UnityEngine;

namespace Forestlevel
{
    public readonly struct MovementContext
    {
        public readonly Vector2 moveInput;
        public readonly bool sprintHeld;
        public readonly bool isGrounded;
        public readonly Vector3 groundNormal;
        public readonly Vector3 currentHorizontalVelocity;
        public readonly float deltaTime;

        public MovementContext(Vector2 moveInput, bool sprintHeld, bool isGrounded,
            Vector3 groundNormal, Vector3 currentHorizontalVelocity, float deltaTime)
        {
            this.moveInput = moveInput;
            this.sprintHeld = sprintHeld;
            this.isGrounded = isGrounded;
            this.groundNormal = groundNormal;
            this.currentHorizontalVelocity = currentHorizontalVelocity;
            this.deltaTime = deltaTime;
        }
    }

}

