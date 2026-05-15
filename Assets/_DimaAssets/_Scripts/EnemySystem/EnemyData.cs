using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Core Stats")]
    [Tooltip("Maximum health points.")]
    public float maxHealth = 50f;

    [Tooltip("Damage dealt per attack.")]
    public float damage = 10f;

    [Tooltip("Movement speed (m/s).")]
    public float speed = 5f;

    [Header("Detection")]
    [Tooltip("Distance at which the enemy notices the player.")]
    public float alertRange = 10f;

    [Tooltip("Seconds the enemy spends in Alert state before attacking.")]
    public float alertDuration = 1f;

    [Header("Patrol")]
    [Tooltip("How far the enemy roams from its spawn point.")]
    public float roamRadius = 10f;

    [Tooltip("Seconds the enemy pauses at each patrol point.")]
    public float idleDuration = 1.5f;

    [Tooltip("Movement multiplier while patrolling (0.5 = half speed).")]
    [Range(0.1f, 1f)]
    public float patrolSpeedMultiplier = 0.5f;

    [Header("Attack: Range & Timing")]
    [Tooltip("Distance at which the enemy stops chasing and begins its attack windup. Should match the strategy's own attack/dash range.")]
    public float attackRange = 3f;

    [Tooltip("Seconds spent winding up before the attack hits.")]
    public float windupDuration = 0.5f;

    [Tooltip("Seconds before the enemy can attack again.")]
    public float attackCooldown = 2f;

    [Tooltip("Maximum distance the player can run before the enemy gives up and returns to Idle.")]
    public float chaseLeashMultiplier = 1.5f;
}
