using UnityEngine;
public interface IEnemyAttackStrategy
{
    float ApproachDuration { get; }
    void OnApproachTarget(Enemy owner, float distanceToPlayer);
    void OnWindup(Enemy owner);
    void OnExecute(Enemy owner, float distanceToPlayer);
    void OnCooldown(Enemy owner, float distanceToPlayer);
    float ExecutingDuration { get; }

    bool IsExecutionComplete { get; }

    bool BeginsWindupAtAnyDistance { get; }
}
public interface IEnemyAttackStateExitHandler
{
    void OnAttackStateExit(Enemy owner, EnemyAttackStateMachine.State state);
}
