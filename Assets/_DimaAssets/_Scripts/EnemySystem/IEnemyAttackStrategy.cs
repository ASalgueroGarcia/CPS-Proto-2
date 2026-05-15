using UnityEngine;

/// <summary>
/// Contract for all enemy attack strategy components.
/// The Enemy orchestrator calls these during the Attack state's sub-phases.
/// Each strategy is a MonoBehaviour so it appears in the Inspector
/// and can expose its own tweakable fields.
/// </summary>
public interface IEnemyAttackStrategy
{
    /// <summary>
    /// Called every frame while the state machine is in the <b>Approaching</b> sub-state.
    /// Override if the enemy needs special positioning (e.g. ranged enemies keeping distance).
    /// </summary>
    void OnApproachTarget(Enemy owner, float distanceToPlayer);

    /// <summary>
    /// Called every frame while the state machine is in the <b>Windup</b> sub-state.
    /// Good place to play charging animations, visual warnings, or sound cues.
    /// </summary>
    void OnWindup(Enemy owner);

    /// <summary>
    /// Called once when the state machine enters the <b>Executing</b> sub-state.
    /// Implement the actual attack here: damage, projectiles, dash, etc.
    /// </summary>
    void OnExecute(Enemy owner, float distanceToPlayer);

    /// <summary>
    /// Called every frame while the state machine is in the <b>Cooldown</b> sub-state.
    /// If the attack needs repositioning between attacks (e.g. ranged keeping distance),
    /// or doing nothing while catching breath, implement it here.
    /// </summary>
    void OnCooldown(Enemy owner, float distanceToPlayer);

    /// <summary>
    /// How many seconds the Executing sub-state should last.
    /// 0 for instant attacks (melee, projectile). >0 for multi-frame attacks (dash, charging heavy).
    /// </summary>
    float ExecutingDuration { get; }
}
