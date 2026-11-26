using UnityEngine;

public struct TweenedValue
{
    public float Current { get; private set; }
    private float target;
    private float unitsPerSecond;

    public TweenedValue(float value)
    {
        Current = value;
        target = value;
        unitsPerSecond = 0f;
    }

    /// <summary>Heads for target, arriving after seconds. 0 seconds arrives on the next Step.</summary>
    public void MoveTo(float newTarget, float seconds)
    {
        target = newTarget;
        unitsPerSecond = seconds > 0f ? Mathf.Abs(target - Current) / seconds : float.PositiveInfinity;
    }

    public bool Step(float deltaTime)
    {
        if (Current == target) return false;
        Current = float.IsPositiveInfinity(unitsPerSecond) ? target : Mathf.MoveTowards(Current, target, unitsPerSecond * deltaTime);
        return true;
    }
}