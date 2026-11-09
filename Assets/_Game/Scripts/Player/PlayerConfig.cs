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
}
