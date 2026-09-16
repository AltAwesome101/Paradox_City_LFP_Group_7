using System;
using UnityEngine;
using BetterEventBus;

[RequireComponent(typeof(Rigidbody))]
public class Apple : MonoBehaviour
{
    [SerializeField] AppleRegistry registry;
    [SerializeField] AppleTrajectoryProjector projector;
    public LayerMask groundMask;
    public LayerMask playerMask;

    bool resolved;    // one flag guarding both outcomes, not just ground
    bool isAirborne;
    AppleDeployer sourceDeployer;
    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;
    }

    void OnEnable() => registry.Register(this);
    void OnDisable() => registry.Deregister(this);

    void OnCollisionEnter(Collision collision)
    {
        if (resolved) return;

        int layer = collision.gameObject.layer;

        if (((1 << layer) & playerMask) != 0) { Resolve(caught: true); return; }
        if (((1 << layer) & groundMask) != 0) { Resolve(caught: false); }
    }

    void Resolve(bool caught)
    {
        resolved = true;
        isAirborne = false;

        if (caught) GameEventBus.Raise(new AppleCollectedEvent(this));
        else GameEventBus.Raise(new AppleDroppedEvent(this));

        sourceDeployer?.MarkFree();
        projector.Hide();   
        AppleDeployManager.Instance.ReturnToPool(this);

    }

    public void StartFalling(AppleDeployer deployer = null)
    {
        sourceDeployer = deployer;
        resolved = false;
        _rb.useGravity = true;
        isAirborne = true;
        GameEventBus.Raise(new AppleAirborneEvent(this));
    }

    void FixedUpdate()
    {
        if (isAirborne) projector.Project(transform, _rb, groundMask);
    }
}