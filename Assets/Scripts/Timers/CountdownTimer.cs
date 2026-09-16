using System;
using UnityEngine;

public class CountdownTimer : Timer
{
    public event Action<float> OnTick;
    
    public CountdownTimer(float value) : base(value)
    {
        CurrentTime = value;
    }


    public override bool IsFinished => CurrentTime <= 0;

    public override void Tick(float deltaTime)
    {
        if(!IsRunning || IsFinished) return;
        CurrentTime -= deltaTime;
        OnTick?.Invoke(CurrentTime<0f? 0f: CurrentTime);

        if(IsFinished) Stop();
    }

    public override void Tick(){}
}
