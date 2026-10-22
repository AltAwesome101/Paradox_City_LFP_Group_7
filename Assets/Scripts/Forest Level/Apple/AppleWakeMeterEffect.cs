using UnityEngine;
using BetterEventBus;

[CreateAssetMenu(fileName ="AppleWakeMeterEffect",menuName = "Apples/Effects/Wake Meter")]
public class AppleWakeMeterEffect : AppleEffect
{
    [SerializeField] int amount = 30;
    public override void Apply(in AppleContext context)
        => GameEventBus.Raise<WakeMeterChangeEvent>(new WakeMeterChangeEvent(amount));
}
