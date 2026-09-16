using System;
using UnityEngine;

public class IntervalTimer : Timer {
    readonly float interval;
    float nextInterval;
    public Action onInterval = delegate { };


    public IntervalTimer(float totalTime, float intervalSeconds) : base(totalTime) {
        interval = intervalSeconds;
        nextInterval = totalTime - interval;
    }


    public override void Tick() {
        if (IsRunning && CurrentTime > 0) {
            CurrentTime -= Time.deltaTime;
        }

        //Fire interval events as  long as thresholds are crossed
        while (CurrentTime <= nextInterval && nextInterval >= 0) {
            onInterval.Invoke();
            nextInterval -= interval;
        }

        if (!IsRunning && CurrentTime <= 0) {
            CurrentTime = 0;
            Stop();
        }

    }

    public override void Tick(float deltaTime){}

    public override bool IsFinished => CurrentTime <= 0;

}
