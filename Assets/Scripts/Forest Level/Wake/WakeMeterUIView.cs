using UnityEngine;
using UnityEngine.UIElements;
using System;
using BetterEventBus;

public class WakeMeterUIView : BaseUIView
{
    public event Action OnMeterFull;
    ProgressBar progress;
    float lerpSpeed;

    float targetValue;
    IVisualElementScheduledItem scheduledItem;

    public WakeMeterUIView(VisualElement container, int maxLimit, float lerpSpeed = 8f): base(container)
    {
        progress = container.Q<ProgressBar>("wake-progressbar");
        progress.lowValue = 0f;
        progress.value = 0f;
    }

    public void Configure(int disturb, int maxLimit, float lerpSpeed)
    {
        progress.highValue = maxLimit;
        this.lerpSpeed = lerpSpeed;
    }

    public override void Show() => progress.style.display = DisplayStyle.Flex;
    public override void Hide() => progress.style.display = DisplayStyle.None;


    public void Disturb(float disturbAmount)
    {
        targetValue = Mathf.Min(targetValue + disturbAmount, progress.highValue);

        scheduledItem?.Pause();
        scheduledItem = progress.schedule
            .Execute(AnimateStep)
            .Every(16)
            .Until(() => Mathf.Approximately(progress.value, targetValue));

        if (targetValue >= progress.highValue)
            OnMeterFull?.Invoke();
    }

    public void ResetMeter(){
        targetValue = 0f;
        progress.value = 0f;
    }

    void AnimateStep()
    {
        progress.value = Mathf.Lerp(progress.value, targetValue, lerpSpeed * 0.016f);
    }
}

public readonly struct WakeMeterFullEvent : IGameplayEvent { }