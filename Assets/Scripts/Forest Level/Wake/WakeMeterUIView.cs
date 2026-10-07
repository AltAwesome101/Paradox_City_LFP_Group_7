using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WakeMeterUIView : BaseUIView
{
    public event Action OnMeterFull;
    readonly ProgressBar progress;
    readonly int disturbAmount;
    readonly float lerpSpeed;

    float targetValue;
    IVisualElementScheduledItem scheduledItem;

    public WakeMeterUIView(VisualElement container, int disturb, int maxLimit, float lerpSpeed = 8f): base(container)
    {
        this.progress = container.Q<ProgressBar>("wake-progressbar");
        this.progress.lowValue = 0f;
        this.progress.value = 0f;
        this.progress.highValue = maxLimit;
        disturbAmount = disturb;
        this.lerpSpeed = lerpSpeed;
        targetValue = 0f;
    }

    public override void Show() => progress.style.display = DisplayStyle.Flex;
    public override void Hide() => progress.style.display = DisplayStyle.None;


    public void Disturb()
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