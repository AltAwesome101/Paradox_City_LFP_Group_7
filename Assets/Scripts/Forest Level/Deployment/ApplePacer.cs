using System.Collections.Generic;
using UnityEngine;

public class ApplePacer
{
    readonly DeployZone zone;
    readonly int maxConcurrent;
    readonly float minSpawnInterval;
    readonly List<AppleDeployer> eligible = new();   // reused each call, no per-frame allocation

    float timeSinceLastSpawn;

    public ApplePacer(DeployZone zone, int maxConcurrent, float minSpawnInterval)
    {
        this.zone = zone;
        this.maxConcurrent = maxConcurrent;
        this.minSpawnInterval = minSpawnInterval;
        timeSinceLastSpawn = minSpawnInterval; // lets the very first spawn happen immediately
    }

    public bool TryGetNextDeployer(float deltaTime, out AppleDeployer deployer)
    {
        deployer = null;
        timeSinceLastSpawn += deltaTime;

        if (timeSinceLastSpawn < minSpawnInterval) return false;

        int busy = zone.BusyCount;
        if (busy >= maxConcurrent)
        {
            Debug.Log($"Pacer blocked: busy count {busy} >= cap {maxConcurrent}");
            return false;
        }

        eligible.Clear();
        foreach (var candidate in zone.GetAllFree())
        {
            if (!HasBusyConflict(candidate))
                eligible.Add(candidate);
        }

        if (eligible.Count == 0)
        {
            Debug.Log($"Pacer blocked: no eligible candidates — Busy Deploy Count:{busy}/{zone.DeployCount}");
            return false;
        }

        deployer = eligible[Random.Range(0, eligible.Count)];
        timeSinceLastSpawn = 0f;
        return true;
    }

    bool HasBusyConflict(AppleDeployer candidate)
    {
        foreach (var pair in zone.ConflictPairs)
        {
            if (pair.a == candidate && pair.b.IsBusy) return true;
            if (pair.b == candidate && pair.a.IsBusy) return true;
        }
        return false;
    }
}