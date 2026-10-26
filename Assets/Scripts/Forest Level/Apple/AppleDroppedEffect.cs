using UnityEngine;
using BetterEventBus;

[CreateAssetMenu(fileName ="AppleDroppedEffect",menuName = "Apples/Effects/Dropped")]
public class AppleDroppedEffect : AppleEffect
{
    public override void Apply(in AppleContext context)
        => GameEventBus.Raise<AppleDroppedEvent>(new AppleDroppedEvent(context.Apple));
}