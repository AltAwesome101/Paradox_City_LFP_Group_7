using System;
using UnityEngine;

public abstract class Timer {
    public float CurrentTime { get; protected set; }
    public bool IsRunning { get; protected set; }
    protected float initialTime;
    public float Progress => Mathf.Clamp(CurrentTime / initialTime, 0, 1);
    public Action OnTimerStart = delegate { };
    public Action OnTimerStop = delegate { };

    protected Timer(float value) {
        initialTime = value;
    }

    public void Start() {
        CurrentTime = initialTime;
        if (!IsRunning) {
            IsRunning = true;
            OnTimerStart.Invoke();
        }
    }

    public void Stop() {
        if (IsRunning) {
            IsRunning = false;
            OnTimerStop.Invoke();
        }
    }



    public abstract void Tick();
    public abstract void Tick(float deltaTime);

    public abstract bool IsFinished { get; }

    public void Resume() => IsRunning = true;
    public void Pause() => IsRunning = true;
    public virtual void Reset() => CurrentTime = initialTime;
    public virtual void Reset(float newTime) {
        initialTime = newTime;
        Reset();
    }


}
