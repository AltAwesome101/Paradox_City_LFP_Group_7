using System;

[Serializable]
public struct AppleSpawnEntry
{
    public AppleDefinitionSO Apple;
    public float Weight;
    public float UnlockAfterSeconds;   // difficulty ramp inside the session
}
