using System;
using UnityEngine;
using BetterEventBus;
using BetterPooling;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class Apple : MonoBehaviour
{
    [SerializeField] AppleDefinitionSO definition;
    public LayerMask groundMask;
    public LayerMask playerMask;
    public AppleDefinitionSO Definition => definition;

    ISingleObjectPool<Apple> pool;


    bool resolved, isAirborne;
    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
    }

    public void Init(AppleDefinitionSO def, ISingleObjectPool<Apple> owner){
        definition = def;
        pool = owner;
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
        foreach(var effect in caught? definition.CaughtEffects : definition.MissedEffects)
            effect.Apply(ctx);
        
        GameEventBus.Raise<IGameplayEvent>(caught? new AppleCollectedEvent(this): new AppleDroppedEvent(this));
        pool.Release(this);
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

[CreateAssetMenu(menuName ="Apples/ Apple Definition")]
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

[CreateAssetMenu(menuName = "Apples/Effects/Wake Meter")]
public class WakeMeterEffect : AppleEffect
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

[CreateAssetMenu(menuName = "Apples/Spawn Table")]
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
