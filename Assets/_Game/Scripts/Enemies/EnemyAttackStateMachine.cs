using UnityEngine;
public class EnemyAttackStateMachine
{
    public enum State { Approaching, Windup, Executing, Cooldown }

    public State CurrentState { get; private set; } = State.Approaching;
    public float StateProgress { get; private set; }

    private float windupDuration;
    private float approachDuration;
    private float executingDuration;
    private float cooldownDuration;
    private float attackRange;
    private float chaseLeashDistance;
    private float stateTimer;

    public void Initialize(float approach, float windup, float execute, float cooldown, float range, float alertRange, float leashMultiplier)
    {
        approachDuration = approach;
        windupDuration = windup;
        executingDuration = execute;
        cooldownDuration = cooldown;
        attackRange = range;
        chaseLeashDistance = alertRange * leashMultiplier;
        Reset();
    }
    public bool Tick(float distanceToTarget, bool executionComplete, bool beginsWindupAtAnyDistance, bool ignoreLeash = false)
    {
        if (!ignoreLeash && distanceToTarget > chaseLeashDistance)
            return false;

        if (CurrentState == State.Executing && executionComplete)
            stateTimer = 0f;
        else
            stateTimer -= Time.deltaTime;
        float totalDuration = GetStateDuration(CurrentState);
        StateProgress = totalDuration > 0 ? Mathf.Clamp01(1f - (stateTimer / totalDuration)) : 1f;
        if (stateTimer <= 0)
        {
            switch (CurrentState)
            {
                case State.Approaching:
                    if (beginsWindupAtAnyDistance || distanceToTarget <= attackRange)
                    {
                        ChangeState(State.Windup);
                    }
                    break;

                case State.Windup:
                    ChangeState(State.Executing);
                    break;

                case State.Executing:
                    ChangeState(State.Cooldown);
                    break;

                case State.Cooldown:
                    ChangeState(State.Approaching);
                    break;
            }
        }

        return true;
    }

    private float GetStateDuration(State state)
    {
        return state switch
        {
            State.Approaching => approachDuration,
            State.Windup => windupDuration,
            State.Executing => executingDuration,
            State.Cooldown => cooldownDuration,
            _ => 0f
        };
    }

    private void ChangeState(State state)
    {
        CurrentState = state;
        stateTimer = GetStateDuration(state);
        StateProgress = 0f;
    }

    public void Reset()
    {
        ChangeState(State.Approaching);
    }
}
