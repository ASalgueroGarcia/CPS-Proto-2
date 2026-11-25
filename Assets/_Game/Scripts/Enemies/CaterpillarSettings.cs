using UnityEngine;

/// <summary>
/// Tuning for the Caterpillar enemy's stats, slam attack, split/reform recovery,
/// and segmented body trail.
/// </summary>
[CreateAssetMenu(fileName = "CaterpillarSettings", menuName = "Enemies/Caterpillar Settings")]
public class CaterpillarSettings : ScriptableObject
{
    [Header("Core Stats")]
    [Tooltip("Maximum shared health of the Caterpillar.")]
    [Min(1f)] public float maxHealth = 40f;
    [Tooltip("Movement speed while pursuing the player, in metres per second.")]
    [Min(0f)] public float moveSpeed = 2f;

    [Header("Detection & Engagement")]
    [Tooltip("Distance at which the Caterpillar permanently becomes aware of the player.")]
    [Min(0f)] public float detectionRange = 5f;
    [Tooltip("Distance within which the Caterpillar stops chasing to wind up or hold on cooldown.")]
    [Min(0f)] public float attackRange = 4f;

    [Header("Slam Attack")]
    [Tooltip("Seconds spent rearing upright with alert eyes while tracking the player.")]
    [Min(0f)] public float windupDuration = 1.2f;
    [Tooltip("Forward fall speed during the slam, in metres per second.")]
    [Min(0.01f)] public float fallSpeed = 5f;
    [Tooltip("Distance ahead along the locked slam direction where the impact circle is centred.")]
    [Min(0f)] public float slamCenterOffset = 2f;
    [Tooltip("Radius of the horizontal slam impact zone, in metres.")]
    [Min(0f)] public float slamImpactRadius = 3f;
    [Tooltip("Damage dealt to targets inside the slam impact radius.")]
    [Min(0f)] public float slamDamage = 25f;
    [Tooltip("Knockback force applied by the slam impact.")]
    [Min(0f)] public float slamKnockbackForce = 6f;

    [Header("Split, Reform & Cooldown")]
    [Tooltip("Seconds the body remains scattered into pieces; only the head piece takes damage.")]
    [Min(0f)] public float splitDuration = 2f;
    [Tooltip("Seconds the scattered pieces take to fly back together.")]
    [Min(0f)] public float reformDuration = 0.5f;
    [Tooltip("Cooldown started once reform finishes before another windup can begin.")]
    [Min(0f)] public float attackCooldown = 3f;

    [Header("Segments & Trail")]
    [Tooltip("Total piece count including the head segment.")]
    [Min(2)] public int segmentCount = 6;
    [Tooltip("Target spacing between consecutive segments along the trail, in metres.")]
    [Min(0.05f)] public float segmentSpacing = 0.55f;
    [Tooltip("Visual and collider radius of each body segment, in metres.")]
    [Min(0.05f)] public float segmentRadius = 0.4f;
    [Tooltip("Visual and collider radius of the head piece, in metres.")]
    [Min(0.05f)] public float headRadius = 0.48f;
    [Tooltip("Radial distance body pieces scatter outward on slam impact, in metres.")]
    [Min(0f)] public float scatterRadius = 2.5f;
    [Tooltip("Exponential smoothing sharpness when segments follow the head's path.")]
    [Min(0.01f)] public float trailFollowSharpness = 12f;

    /// <summary>Duration of the forward slam fall derived from slamCenterOffset / fallSpeed.</summary>
    public float SlamDuration => slamCenterOffset > 0f && fallSpeed > 0f ? slamCenterOffset / fallSpeed : 0f;
}
