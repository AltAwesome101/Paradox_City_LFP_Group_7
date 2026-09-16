using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
    // Default movement, used when not "in game" — full 3D exploration/traversal
    // where the player can move in any horizontal direction relative to the
    // camera. Owns the camera-lock-basis behaviour that used to live directly
    // on PlayerMovement: once the player starts moving, "forward" freezes to
    // whichever way the camera faced at that instant, so turning the player
    // doesn't drag the movement basis around with it.
    public class TraversalMovement : IMovementStrategy
    {
        const float lockEnterSpeed = 0.15f;
        const float lockExitSpeed = 0.05f;

        PlayerMovement owner;
        Quaternion lockedBasis = Quaternion.identity;

        public bool IsLocked { get; private set; }

        public void OnEnter(PlayerMovement owner)
        {
            this.owner = owner;
            IsLocked = false; // re-entering traversal never inherits a stale lock
            GameEventBus.Raise<CameraChangeEvent>(new CameraChangeEvent(CameraType.FreeLook));
        }

        public void OnExit()
        {
            owner = null;
        }

        public Vector3 GetHorizontalTarget(in MovementContext ctx)
        {
            UpdateLockState(ctx);

            Vector3 forward, right;
            var cam = owner.CameraTransform;

            if (IsLocked)
            {
                forward = lockedBasis * Vector3.forward;
                right = lockedBasis * Vector3.right;
            }
            else
            {
                if (cam == null) return Vector3.zero;
                forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            }

            Vector3 dir = forward * ctx.moveInput.y + right * ctx.moveInput.x;
            return dir.magnitude > 1f ? dir.normalized : dir;
        }

        void UpdateLockState(in MovementContext ctx)
        {
            // Uses horizontal velocity only — see note above re: original
            // using full rb velocity and false-triggering off fall speed.
            float speedSqr = ctx.currentHorizontalVelocity.sqrMagnitude;
            bool wasLocked = IsLocked;

            if (!IsLocked && speedSqr > lockEnterSpeed * lockEnterSpeed) IsLocked = true;
            else if (IsLocked && speedSqr < lockExitSpeed * lockExitSpeed) IsLocked = false;

            if (IsLocked && !wasLocked)
            {
                var cam = owner.CameraTransform;
                Vector3 flatForward = cam != null
                    ? Vector3.ProjectOnPlane(cam.forward, Vector3.up)
                    : owner.transform.forward;

                if (flatForward.sqrMagnitude < 0.0001f)
                    flatForward = Vector3.ProjectOnPlane(owner.transform.forward, Vector3.up);

                lockedBasis = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            }
        }
    }

}

