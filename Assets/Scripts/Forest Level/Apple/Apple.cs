using System;
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

[CreateAssetMenu(fileName ="AppleDefinition",menuName ="Apples/ Apple Definition")]
public class AppleDefinitionSO : ScriptableObject
{
    public Apple Prefab;
    public int ProgressValue = 1;
    public Color AppleColor;
    public AppleEffect[] CaughtEffects;
    public AppleEffect[] MissedEffects;


}

public abstract class AppleEffect: ScriptableObject
{
    public abstract void Apply(in AppleContext context);
}

public readonly  struct AppleContext
{
    public readonly Apple Apple;
    public readonly Vector3 Position;
    public AppleContext(Apple apple, Vector3 position){
        Apple = apple;
        Position = position;
    }
}

[CreateAssetMenu(fileName ="AppleWakeMeterEffect",menuName = "Apples/Effects/Wake Meter")]
public class AppleWakeMeterEffect : AppleEffect
{
    [SerializeField] int amount = 30;
    public override void Apply(in AppleContext context)
        => GameEventBus.Raise<WakeMeterChangeEvent>(new WakeMeterChangeEvent(amount));
}

[Serializable]
public struct AppleSpawnEntry
{
    public AppleDefinitionSO Apple;
    public float Weight;
    public float UnlockAfterSeconds;   // difficulty ramp inside the session
}

[CreateAssetMenu(fileName ="AppleSpawnTable",menuName = "Apples/Spawn Table")]
public class AppleSpawnTable : ScriptableObject
{
    [SerializeField] AppleSpawnEntry[] entries;

    public AppleDefinitionSO Pick(float sessionTime)
    {
        float total = 0f;
        foreach (var e in entries) if (sessionTime >= e.UnlockAfterSeconds) total += e.Weight;

        float roll = UnityEngine.Random.value * total;
        foreach (var e in entries)
        {
            if (sessionTime < e.UnlockAfterSeconds) continue;
            if ((roll -= e.Weight) <= 0f) return e.Apple;
        }
        return entries[0].Apple; // fallback
    }
}
