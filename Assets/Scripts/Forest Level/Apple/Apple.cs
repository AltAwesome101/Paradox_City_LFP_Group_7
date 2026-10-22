using UnityEngine;
using BetterEventBus;
using BetterPooling;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class Apple : MonoBehaviour
{
    public LayerMask groundMask;
    public LayerMask playerMask;
    public AppleDefinitionSO Definition {get; private set;}
    IAppleOwner owner;


    bool resolved, isAirborne;
    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
    }

    public void Init(AppleDefinitionSO def, IAppleOwner owner){
        Definition = def;
        this.owner = owner;
    }

    void OnTriggerEnter(Collider other)
    {
        if (resolved) return;

        int layer = other.gameObject.layer;

        if (((1 << layer) & playerMask) != 0) { Resolve(caught: true); return; }
        if (((1 << layer) & groundMask) != 0) { Resolve(caught: false); }
    }

    void Resolve(bool caught)
    {
        resolved = true;
        isAirborne = false;

        var ctx = new AppleContext(this, transform.position);
        foreach(var effect in caught? Definition.CaughtEffects : Definition.MissedEffects)
            effect.Apply(ctx);
        
        if (caught) GameEventBus.Raise(new AppleCollectedEvent(this));
        else        GameEventBus.Raise(new AppleDroppedEvent(this));

        owner.Release(this);   // last, so effects and listeners still see a live apple
    }

    public void Drop()
    {
        resolved = false;
        _rb.useGravity = true;
        isAirborne = true;
        GameEventBus.Raise(new AppleAirborneEvent(this));
    }

    public void ResetPhysics()
    {
        resolved = true; isAirborne = false;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

}
