using UnityEngine;
using BetterEventBus;

[CreateAssetMenu(fileName ="AppleCollectionEffect",menuName = "Apples/Effects/AppleCaught")]
public class AppleCollectionEffect : AppleEffect
{
    public override void Apply(in AppleContext context)
        => GameEventBus.Raise<AppleCollectedEvent>(new AppleCollectedEvent(context.Apple));
}