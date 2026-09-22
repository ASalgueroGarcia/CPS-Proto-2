using UnityEngine;

/// <summary>
/// Lightweight state machine for the Attack phase shared by all enemies.
/// Handles the universal cycle: Approach -> Windup -> Execute -> Cooldown.
/// The owning EnemyBase reads CurrentState each frame and calls the matching
/// virtual method (OnApproachTarget, OnAttackWindup, OnAttackExecute, OnAttackCooldown).
/// </summary>
public class EnemyAttackStateMachine
{
    public enum State { Approaching, Windup, Executing, Cooldown }

    public State CurrentState { get; private set; } = State.Approaching;

    /// <summary>
    /// Normalized progress (0..1) through the current state's timer.
    /// Useful for charging animations.
    /// </summary>
    public float StateProgress { get; private set; }

    private float windupDuration;
    private float executingDuration;
    private float cooldownDuration;
    private float attackRange;
    private float chaseLeashDistance;
    private float stateTimer;

    public void Initialize(float windup, float execute, float cooldown, float range, float alertRange, float leashMultiplier)
    {
        windupDuration = windup;
        executingDuration = execute;
        cooldownDuration = cooldown;
        attackRange = range;
        chaseLeashDistance = alertRange * leashMultiplier;
        Reset();
    }

    /// <summary>
    /// Call every frame. Returns true if the target is still in leash range;
    /// false means the enemy should drop to Idle (target too far).
    /// </summary>
    public bool Tick(float distanceToTarget)
    {
        if (distanceToTarget > chaseLeashDistance)
            return false;

        stateTimer -= Time.deltaTime;
        float totalDuration = GetStateDuration(CurrentState);
        StateProgress = totalDuration > 0 ? 1f - (stateTimer / totalDuration) : 1f;

        // Auto-transition when timer expires
        if (stateTimer <= 0)
        {
            switch (CurrentState)
            {
                case State.Approaching:
                    if (distanceToTarget <= attackRange)
                    {
                        CurrentState = State.Windup;
                        stateTimer = windupDuration;
                    }
                    break;

                case State.Windup:
                    CurrentState = State.Executing;
                    stateTimer = executingDuration;
                    break;

                case State.Executing:
                    CurrentState = State.Cooldown;
                    stateTimer = cooldownDuration;
                    break;

                case State.Cooldown:
                    CurrentState = State.Approaching;
                    stateTimer = 0f;
                    break;
            }
        }

        return true;
    }

    private float GetStateDuration(State state)
    {
        return state switch
        {
            State.Windup => windupDuration,
            State.Executing => executingDuration,
            State.Cooldown => cooldownDuration,
            _ => 0f
        };
    }

    public void Reset()
    {
        CurrentState = State.Approaching;
        stateTimer = 0f;
        StateProgress = 0f;
    }
}
