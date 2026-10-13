using System;

public class SuccessEventPredicate : IPredicate
{
    private readonly IPredicate condition;
    private readonly Action onSuccess;
    private bool completed;

    public SuccessEventPredicate(
        IPredicate condition,
        Action onSuccess)
    {
        this.condition = condition;
        this.onSuccess = onSuccess;
    }

    public bool Evaluate()
    {
        if (completed)
            return true;

        if (!condition.Evaluate())
            return false;

        completed = true;
        onSuccess?.Invoke();

        return true;
    }
}