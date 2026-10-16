using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on any AI that should react to sounds (Hitler's AI script).
/// Called when a stone lands (or a loud weapon is fired) within hearing range.
/// </summary>
public interface IHearsNoise
{
    void OnNoiseHeard(Vector3 position, float radius);
}

/// <summary>
/// Optional: implement on anything that should take damage from the player's
/// weapons. (KnightAI is already supported without this.)
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}

public static class NoiseSystem
{
    static readonly HashSet<IHearsNoise> notified = new HashSet<IHearsNoise>();

    /// <summary>Makes a noise at 'position' that every IHearsNoise within 'radius' will hear.</summary>
    public static void Emit(Vector3 position, float radius)
    {
        if (radius <= 0f)
            return;

        notified.Clear();
        Collider[] cols = Physics.OverlapSphere(position, radius, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider c in cols)
        {
            IHearsNoise listener = c.GetComponentInParent<IHearsNoise>();
            if (listener != null && notified.Add(listener))
                listener.OnNoiseHeard(position, radius);
        }
    }
}