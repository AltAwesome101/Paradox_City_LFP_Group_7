using UnityEngine;

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
