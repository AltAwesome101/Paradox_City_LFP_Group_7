using UnityEngine;

public abstract class AppleEffect: ScriptableObject
{
    public abstract void Apply(in AppleContext context);
}
