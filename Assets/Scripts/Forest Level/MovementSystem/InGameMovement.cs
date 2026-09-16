using UnityEngine;
using BetterEventBus;

namespace Forestlevel
{
    // In-game movement — constrained 2D lane movement. Only A/D (moveInput.x)
    // matters; W/S is discarded, not just left unused, so nothing upstream can
    // accidentally leak forward/back input through here.
    //
    // ASSUMPTION: "right" comes from PlayerMovement.CameraTransform, flattened.
    // Once the dedicated fixed-camera system exists, swap this for that
    // reference instead — likely follow-up, not done here.
    public class InGameMovement : IMovementStrategy
    {
        PlayerMovement owner;

        public void OnEnter(PlayerMovement owner)
        {
            this.owner = owner;
            GameEventBus.Raise<CameraChangeEvent>(new CameraChangeEvent(CameraType.InGame));
        }

        public void OnExit()
        {
            owner = null;
        }

        public Vector3 GetHorizontalTarget(in MovementContext ctx)
        {
            var cam = owner.CameraTransform;
            Vector3 right = cam != null
                ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized
                : Vector3.right; // no camera assigned — fall back to world axis

            Vector3 dir = right * ctx.moveInput.x;
            return dir.magnitude > 1f ? dir.normalized : dir;
        }
    }

}

