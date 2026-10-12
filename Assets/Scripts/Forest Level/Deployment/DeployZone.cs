using System.Collections.Generic;
using BetterEventBus;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeployZone : MonoBehaviour
{
    [SerializeField] DeployZoneRegistry registry;
    [SerializeField] LayerMask playerMask;
    [SerializeField] string promptText = "Press E to start";
    [SerializeField] List<DeployerPair> conflictPairs;

    readonly List<AppleDeployer> _deployers = new();
    public IReadOnlyList<AppleDeployer> Deployers => _deployers;
    public IReadOnlyList<DeployerPair> ConflictPairs => conflictPairs;
    public string PromptText => promptText;
    public bool IsCompleted { get; set; }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        GetComponentsInChildren(_deployers);   // clears and refills the list
    }

    void OnEnable()  => registry.Register(this);
    void OnDisable() => registry.Deregister(this);

    void OnTriggerEnter(Collider other)
    {
        if (InMask(other)) GameEventBus.Raise(new DeployZoneRangeEvent(this, true));
    }
    void OnTriggerExit(Collider other)
    {
        if (InMask(other)) GameEventBus.Raise(new DeployZoneRangeEvent(this, false));
    }
    bool InMask(Collider c) => (playerMask.value & (1 << c.gameObject.layer)) != 0;

    // moved over from DeployRegistry
    public IEnumerable<AppleDeployer> GetAllFree()
    {
        foreach (var d in _deployers) if (!d.IsBusy) yield return d;
    }

    public int DeployCount => _deployers.Count;

    public int BusyCount
    {
        get
        {
            int count = 0;
            foreach (var d in _deployers)
                if (d.IsBusy) count++;
            return count;
        }
    }
}