using BetterEventBus;
using UnityEngine;

namespace Forestlevel
{
    // What a movement strategy actually needs from whatever owns it. Decouples the
    // strategies from any one concrete controller class (PlayerMovement, ForestPlayerController,
    // or whatever comes next) - satisfied automatically by any MonoBehaviour since
    // `transform` is already inherited from Component.
    public interface IMovementOwner
    {
        Transform CameraTransform { get; }
        Transform transform { get; }
    }

    public interface IMovementStrategy : IGameplayEvent
    {
        void OnEnter(IMovementOwner owner);
        void OnExit();
        Vector3 GetHorizontalTarget(in MovementContext ctx);
    }
}