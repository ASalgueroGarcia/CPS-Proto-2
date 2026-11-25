using UnityEngine;

/// <summary>
/// Shared ScriptableObject holding the player's tunable stats.
/// Single source of truth consumed by <see cref="Player"/>, <see cref="PlayerLocomotion"/>,
/// and <see cref="PlayerDashController"/> so values are no longer duplicated across modules.
/// </summary>
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Player/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Base horizontal movement speed (m/s).")]
    public float speed = 14f;

    [Tooltip("Speed applied during a dash (m/s).")]
    public float dashSpeed = 30f;

    [Tooltip("Gravity acceleration applied when not grounded (m/s^2).")]
    public float gravity = 25f;

    [Header("Animation")]
    [Tooltip("Animator.speed multiplier used while running.")]
    public float runAnimationSpeed = 1.5f;

    [Header("Dash")]
    [Tooltip("Total duration of a dash in seconds.")]
    public float dashDuration = 0.2f;

    [Header("Combat")]
    [Tooltip("Seconds the player can be stuck in an attack state before the combo is force-reset.")]
    public float attackFailsafeSeconds = 3.0f;

    [Header("Pit Fall & Respawn")]
    [Tooltip("Damage applied when respawning after dashing into a pit.")]
    [Min(0f)]
    public float pitFallDamage = PitFallRules.DefaultDamage;

    [Tooltip("Duration of the drop animation into the pit before respawning (seconds).")]
    [Min(0.05f)]
    public float fallDuration = PitFallRules.DefaultFallDuration;

    [Tooltip("Vertical distance the player drops into the pit during the fall animation (meters).")]
    [Min(0.1f)]
    public float fallDepth = PitFallRules.DefaultFallDepth;

    [Tooltip("Minimum horizontal clearance from the pit edge when respawning at the last safe position (meters).")]
    [Min(0f)]
    public float pitRespawnClearance = PitFallRules.DefaultRespawnClearance;

    [Tooltip("Duration of the semi-transparent-to-opaque respawn blink effect (seconds).")]
    [Min(0f)]
    public float respawnBlinkDuration = PitFallRules.DefaultBlinkDuration;

    [Tooltip("Half-cycle interval of each semi-transparent/opaque oscillation during respawn blink (seconds).")]
    [Min(0.02f)]
    public float respawnBlinkInterval = PitFallRules.DefaultBlinkInterval;

    [Tooltip("Minimum alpha reached at the transparent trough of each respawn blink cycle.")]
    [Range(0.05f, 1f)]
    public float respawnMinAlpha = PitFallRules.DefaultMinAlpha;

    [Tooltip("Brief invulnerability window granted after pit respawn damage is applied (seconds).")]
    [Min(0f)]
    public float respawnInvulnerabilityDuration = PitFallRules.DefaultInvulnerabilityDuration;
}
