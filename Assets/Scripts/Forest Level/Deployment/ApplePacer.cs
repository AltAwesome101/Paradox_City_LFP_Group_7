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

        if (timeSinceLastSpawn < minSpawnInterval) return false; // expected, ignore for now

        if (registry.BusyCount >= maxConcurrent)
        {
            Debug.Log($"Pacer blocked: busy count {registry.BusyCount} >= cap {maxConcurrent}");
            return false;
        }

        bool anyCandidates = false;
        foreach (var candidate in registry.GetAllFree())
        {
            anyCandidates = true;
            if (HasBusyConflict(candidate)) continue;

            deployer = candidate;
            timeSinceLastSpawn = 0f;
            return true;
        }

        if (!anyCandidates)
            Debug.Log($"Pacer blocked: registry.GetAllFree() returned nothing,  Busy Deploy Count:{registry.BusyCount}/{registry.DeployCount} — no deployers registered?");

        return false;
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
