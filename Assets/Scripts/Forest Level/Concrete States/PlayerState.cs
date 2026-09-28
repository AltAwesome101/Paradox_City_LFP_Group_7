using Forestlevel;
using UnityEngine;

public  abstract class PlayerState : BaseState
{
    ///<summary> Shared state behaviour and constructor requirements for concrete behaviour. </summary>
    protected readonly PlayerMotion motion;
    protected readonly AnimatorController anim;

    protected PlayerState(EntityController entity, PlayerMotion motion, AnimatorController anim) : base(entity)
    {
        this.motion = motion;
        this.anim = anim;
    }
}

public class FreeRoamState : PlayerState
{
    /// <summary>
    ///  Normal 3D traversal walking. Making use of Traversal Movement from MovementStrategy
    /// </summary>
    
    readonly InputReaderSO input;
    readonly WorldStateSensor sensor;
    readonly IMovementOwner owner;
    IMovementStrategy traversalStrategy;

    public FreeRoamState(EntityController entity, PlayerMotion motion, AnimatorController anim,InputReaderSO input,WorldStateSensor sensor, IMovementOwner owner) : base(entity, motion, anim)
    {
        this.sensor = sensor;
        this.input = input;
        this.owner = owner;
        traversalStrategy = new TraversalMovement();
    }

    public override void OnEnter()
    {
        base.OnEnter();
        traversalStrategy.OnEnter(owner);
    }

    public override void Update()
    {
        base.Update();
        var deltaTime = Time.deltaTime;
        motion.RefreshSurface();

        var ctx = new MovementContext(moveInput: input.MoveDirection, 
                                    sprintHeld: input.IsSprintHeld,
                                    isGrounded: motion.OnSurface,
                                    groundNormal: Vector3.up
                                    ,currentHorizontalVelocity: motion.HorizontalVelocity,
                                    deltaTime: deltaTime);

        traversalStrategy.GetHorizontalTarget(ctx);

    }

}

public class AppleCatchingState : PlayerState
{
    /// <summary>
    ///  InGame 2D traversal movement. Making use of In Game Movement from MovementStrategy
    /// Player catching animation occurs here with help with world state sensor
    /// </summary>
    
    readonly InputReaderSO input;
    //TODO: Add Sensor
    readonly IMovementOwner owner;
    IMovementStrategy InGameStrategy;

    public AppleCatchingState(EntityController entity, PlayerMotion motion, AnimatorController anim,InputReaderSO input, IMovementOwner owner) : base(entity, motion, anim)
    {
        this.input = input;
        this.owner = owner;
        InGameStrategy = new InGameMovement();
    }

    public override void OnEnter()
    {
        base.OnEnter();
        InGameStrategy.OnEnter(owner);
    }

    public override void Update()
    {
        base.Update();

        var deltaTime = Time.deltaTime;
        motion.RefreshSurface();

        var ctx = new MovementContext(moveInput: input.MoveDirection, 
                                    sprintHeld: input.IsSprintHeld,
                                    isGrounded: motion.OnSurface,
                                    groundNormal: Vector3.up
                                    ,currentHorizontalVelocity: motion.HorizontalVelocity,
                                    deltaTime: deltaTime);

        InGameStrategy.GetHorizontalTarget(ctx);
    }

}

public class LockedState : PlayerState
{
    /// <summary>
    ///  LockedState motion meaning no input, no movement
    /// </summary>
    
    public LockedState(EntityController entity, PlayerMotion motion, AnimatorController anim) : base(entity, motion, anim)
    {
    }

    public override void Update()
    {
        base.Update();

        motion.RefreshSurface();
        motion.Move(Vector3.zero,Vector3.zero, Time.deltaTime);

        anim.StopMovement();
        anim.SetOnSurface(motion.OnSurface);
    }
}


