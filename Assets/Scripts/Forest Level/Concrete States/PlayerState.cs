using BetterEventBus;
using Forestlevel;
using UnityEngine;

public abstract class PlayerState : BaseState
{
    /// <summary>Shared state behaviour and constructor requirements for concrete behaviour.</summary>
    protected readonly PlayerMotion motion;
    protected readonly AnimatorController anim;

    protected PlayerState(EntityController entity, PlayerMotion motion, AnimatorController anim) : base(entity)
    {
        this.motion = motion;
        this.anim = anim;
    }

    /// <summary>
    /// Shared per-frame locomotion: reads whatever strategy is currently live on the
    /// owner (not one the state instantiated itself - see class-level notes on
    /// FreeRoamState), moves the motor, and drives the movement/ground animator params.
    /// Returns the wish direction so a state can layer extra logic on top (ledges, poses).
    /// </summary>
    protected Vector3 Locomote(IMovementOwner owner, InputReaderSO input)
    {
        motion.RefreshSurface();

        var ctx = new MovementContext(
            moveInput: input.MoveDirection,
            sprintHeld: input.IsSprintHeld,
            isGrounded: motion.OnSurface,
            groundNormal: Vector3.up,
            currentHorizontalVelocity: motion.HorizontalVelocity,
            deltaTime: Time.deltaTime);

        Vector3 wishDir = owner.MovementStrategy?.GetHorizontalTarget(ctx) ?? Vector3.zero;

        motion.Move(wishDir * motion.MovementSpeed, wishDir, Time.deltaTime);
        anim.SetMovement(Mathf.Clamp01(wishDir.magnitude));
        anim.SetOnSurface(motion.OnSurface);

        // Debug.Log($"move:{input.MoveDirection} strategy:{(owner.MovementStrategy == null ? "NULL" : owner.MovementStrategy.GetType().Name)} cam:{(owner.CameraTransform == null ? "NULL" : "ok")}");
        return wishDir;
    }
}

public class FreeRoamState : PlayerState
{
    /// <summary>
    /// Normal 3D traversal. On enter, raises TraversalMovement as the default strategy -
    /// through the SAME event bus a parkour system would use - rather than owning an
    /// instance directly. Update always reads owner.MovementStrategy fresh, so if parkour
    /// (or anything else) pushes a different IMovementStrategy mid-state, this state picks
    /// it up automatically without needing to know parkour exists.
    /// </summary>

    readonly InputReaderSO input;
    readonly WorldStateSensor sensor;
    readonly IMovementOwner owner;

    public FreeRoamState(EntityController entity, PlayerMotion motion, AnimatorController anim, InputReaderSO input, WorldStateSensor sensor, IMovementOwner owner)
        : base(entity, motion, anim)
    {
        this.input = input;
        this.sensor = sensor;
        this.owner = owner;
    }

    // Restored: the game-state events (ExplorationGameStateEvent etc.) no longer raise
    // IMovementStrategy themselves - camera/teleport/cursor only. This state owns picking
    // the default strategy for Exploration now.
    public override void OnEnter()
    {
        base.OnEnter();
        GameEventBus.Raise<IMovementStrategy>(new TraversalMovement());
        Debug.Log("PlayerState: FreeRoamState");
    }

    public override void Update()
    {
        base.Update();
        Vector3 wishDir = Locomote(owner, input);

        // Stop at a ledge in the movement direction. NOTE: WorldStateSensor polls using
        // transform.forward on a timer, not the live wishDir every frame - fine for a
        // walking pace, but if strafing near a ledge needs frame-accurate detection,
        // call environmentChecker.CheckLedge(wishDir, ...) directly here instead of
        // trusting the sensor's cached facing-direction result.
        if (sensor != null && sensor.LedgeAhead && wishDir.sqrMagnitude > 0.0001f
            && Vector3.Angle(sensor.LedgeInfo.surfaceHit.normal, wishDir) < 90f)
        {
            motion.Move(Vector3.zero, wishDir, Time.deltaTime);
        }
    }
}

public class AppleCatchingState : PlayerState
{
    /// <summary>
    /// InGame 2D traversal + the catch pose. The pose used to be its own MonoBehaviour;
    /// it's really just one detail of "what happens while in this state", so it lives
    /// here as plain fields/logic instead - no component, no separate lifecycle to wire up.
    /// </summary>

    readonly InputReaderSO input;
    readonly WorldStateSensor sensor;
    readonly IMovementOwner owner;
    readonly float catchEnterRadius;
    readonly float catchExitRadius;

    bool handsUp;

    public AppleCatchingState(EntityController entity, PlayerMotion motion, AnimatorController anim,
        InputReaderSO input, WorldStateSensor sensor, IMovementOwner owner,
        float catchEnterRadius, float catchExitRadius)
        : base(entity, motion, anim)
    {
        this.input = input;
        this.sensor = sensor;
        this.owner = owner;
        this.catchEnterRadius = catchEnterRadius;
        this.catchExitRadius = catchExitRadius;
    }

    // Restored for the same reason as FreeRoamState: InGameGameStateEvent no longer
    // raises InGameMovement itself.
    public override void OnEnter()
    {
        base.OnEnter();
        GameEventBus.Raise<IMovementStrategy>(new InGameMovement());
        handsUp = false;
    }

    public override void Update()
    {
        base.Update();
        // Apple.cs's own trigger collider (against playerMask) handles detection and
        // raises AppleCollectedEvent itself - nothing to tick here for catching.
        Locomote(owner, input);
        UpdateCatchPose();
    }

    void UpdateCatchPose()
    {
        // Same enter/exit hysteresis as before, just no MonoBehaviour or pending-apple
        // bookkeeping here - WorldStateSensor already owns that.
        float radius = handsUp ? catchExitRadius : catchEnterRadius;
        bool near = sensor.IsAppleNear(radius);
        if (near == handsUp) return;

        handsUp = near;
        anim.SetHandsUp(handsUp);
    }

    public override void OnExit()
    {
        base.OnExit();
        if (!handsUp) return;
        handsUp = false;
        anim.SetHandsUp(false);
    }
}

public class LockedState : PlayerState
{
    /// <summary>No input, no movement. Used for Won/Lost - motor stays enabled so gravity
    /// still applies, it just never receives a wish direction.</summary>

    public LockedState(EntityController entity, PlayerMotion motion, AnimatorController anim)
        : base(entity, motion, anim)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        motion.HoldCurrentRotation(); // don't keep rotating toward stale input
    }

    public override void Update()
    {
        base.Update();
        motion.RefreshSurface();
        motion.Move(Vector3.zero, Vector3.zero, Time.deltaTime);
        anim.StopMovement();
        anim.SetOnSurface(motion.OnSurface);
    }
}