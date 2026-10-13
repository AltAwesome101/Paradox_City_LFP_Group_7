using System;
using UnityEngine;

public class HeldPredicate : IPredicate
{
    float required;
    float held;

    Func<bool> condition;

    public HeldPredicate(Func<bool> condition, float required){
        this.condition = condition;
        this.required = required;
    }

    public bool Evaluate()
    {
        held = condition()? held+ Time.unscaledDeltaTime : 0f;
        return held >= required;
    }
}
