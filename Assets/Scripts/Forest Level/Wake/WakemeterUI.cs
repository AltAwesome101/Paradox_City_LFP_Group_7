using UnityEngine;
using UnityEngine.UIElements;
using System;

public class WakemeterUI
{
    public event Action OnMeterFull;
    readonly ProgressBar progress;
    readonly int disturbAmount;
    readonly float lerpSpeed;

    float targetValue;
    IVisualElementScheduledItem scheduledItem;

    public WakemeterUI(ProgressBar progress, int disturb, int maxLimit, float lerpSpeed = 8f)
    {
        this.progress = progress;
        this.progress.lowValue = 0;
        this.progress.highValue = maxLimit;
        disturbAmount = disturb;
        this.lerpSpeed = lerpSpeed;
        targetValue = progress.value;
    }

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

    void AnimateStep()
    {
        progress.value = Mathf.Lerp(progress.value, targetValue, lerpSpeed * 0.016f);
    }
}