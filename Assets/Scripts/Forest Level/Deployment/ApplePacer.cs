using System.Collections.Generic;
using UnityEngine;

public class ApplePacer
{
    readonly DeployRegistry registry;
    readonly List<DeployerPair> conflictPairs;
    readonly int maxConcurrent;
    readonly float minSpawnInterval;

    float timeSinceLastSpawn;

    public ApplePacer(DeployRegistry registry, List<DeployerPair> conflictPairs, int maxConcurrent, float minSpawnInterval)
    {
        this.registry = registry;
        this.conflictPairs = conflictPairs ?? new List<DeployerPair>();
        this.maxConcurrent = maxConcurrent;
        this.minSpawnInterval = minSpawnInterval;
        timeSinceLastSpawn = minSpawnInterval; // lets the very first spawn happen immediately
    }

    public bool TryGetNextDeployer(float deltaTime, out AppleDeployer deployer)
    {
        deployer = null;
        timeSinceLastSpawn += deltaTime;

        if (timeSinceLastSpawn < minSpawnInterval) return false;

        if (registry.BusyCount >= maxConcurrent)
        {
            Debug.Log($"Pacer blocked: busy count {registry.BusyCount} >= cap {maxConcurrent}");
            return false;
        }

        var eligible = new List<AppleDeployer>();
        foreach (var candidate in registry.GetAllFree())
        {
            if (!HasBusyConflict(candidate))
                eligible.Add(candidate);
        }

        if (eligible.Count == 0)
        {
            Debug.Log($"Pacer blocked: no eligible candidates — Busy Deploy Count:{registry.BusyCount}/{registry.DeployCount}");
            return false;
        }

        deployer = eligible[Random.Range(0, eligible.Count)];
        timeSinceLastSpawn = 0f;
        return true;
    }

    bool HasBusyConflict(AppleDeployer candidate)
    {
        foreach (var pair in conflictPairs)
        {
            if (pair.a == candidate && pair.b.IsBusy) return true;
            if (pair.b == candidate && pair.a.IsBusy) return true;
        }
        return false;
    }
}
