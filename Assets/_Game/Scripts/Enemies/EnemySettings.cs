using System;
using UnityEngine;
[Serializable]
public class EnemySettings
{
    [Header("Core Stats")]
    [Min(1f)] [Tooltip("Maximum health at spawn.")]
    public float maxHealth = 50f;
    [Min(0f)] [Tooltip("Damage dealt per attack.")]
    public float damage = 10f;

    [Header("Movement")]
    [InspectorName("Follow Speed")]
    [Min(0f)] [Tooltip("Movement speed while pursuing the player, in metres per second.")]
    public float speed = 5f;

    [Header("Detection")]
    [InspectorName("Detection Radius")]
    [Min(0f)] [Tooltip("Distance at which the enemy detects the player.")]
    public float alertRange = 10f;
    [Min(0f)] [Tooltip("Time spent alerted before starting the attack cycle.")]
    public float alertDuration = 1f;
    [Tooltip("Keep pursuing after detection or damage, ignoring the chase leash.")]
    public bool awarenessIsPermanent;

    [Header("Patrol")]
    [Min(0f)] [Tooltip("Distance the enemy can roam around a patrol point.")]
    public float roamRadius = 10f;
    [Min(0f)] [Tooltip("Pause between patrol movements.")]
    public float idleDuration = 1.5f;
    [Range(0.1f, 1f)] [Tooltip("Patrol speed as a fraction of Follow Speed.")]
    public float patrolSpeedMultiplier = 0.5f;

    [Header("Attack Timing")]
    [Min(0f)] [Tooltip("Windup trigger distance for range-based attacks. Timed charges do not use this value.")]
    public float attackRange = 3f;
    [Min(0f)] [Tooltip("Time spent warning the player before executing the attack.")]
    public float windupDuration = 0.5f;
    [InspectorName("Recovery Duration")]
    [Min(0f)] [Tooltip("Recovery time before the next approach phase.")]
    public float attackCooldown = 2f;
    [Min(0f)] [Tooltip("Detection Radius multiplier defining the chase leash. Unused with Permanent Awareness.")]
    public float chaseLeashMultiplier = 1.5f;

    public float PatrolSpeed => speed * patrolSpeedMultiplier;
}
