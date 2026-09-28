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
    
    //TODO: Add input Reader
    //TODO: Add Sensor
    readonly IMovementOwner owner;
    IMovementStrategy traversalStrategy;

    public FreeRoamState(EntityController entity, PlayerMotion motion, AnimatorController anim, IMovementOwner owner) : base(entity, motion, anim)
    {
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

        motion.RefreshSurface();

        // var ctx = new MovementContext();


    }

}

public class AppleCatchingState : PlayerState
{
    /// <summary>
    ///  InGame 2D traversal movement. Making use of In Game Movement from MovementStrategy
    /// </summary>
    
    //TODO: Add input Reader
    //TODO: Add Sensor
    readonly IMovementOwner owner;
    IMovementStrategy InGameStrategy;

    public AppleCatchingState(EntityController entity, PlayerMotion motion, AnimatorController anim, IMovementOwner owner) : base(entity, motion, anim)
    {
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

        motion.RefreshSurface();

        // var ctx = new MovementContext();


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


