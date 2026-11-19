using UnityEngine;

/// <summary>
/// ScriptableObject holding the tunable combat stats for the scissors weapon.
/// Consumed by <see cref="ScissorsWeapon"/>; hitbox GameObjects and audio clips
/// remain on the MonoBehaviour because they are scene-bound references.
/// </summary>
[CreateAssetMenu(fileName = "ScissorsWeaponData", menuName = "Weapons/Scissors Weapon Data")]
public class ScissorsWeaponData : ScriptableObject
{
    [Header("Combat Stats")]
    [Tooltip("Base damage before combo multipliers are applied.")]
    public float weaponBaseDamage = 10f;

    [Tooltip("Base critical hit chance (0..1).")]
    public float baseCritChance = 0.05f;

    [Tooltip("Range used for the light/combo attacks and the gizmo preview.")]
    public float attackRange = 2.0f;

    [Tooltip("Radius of the special attack AOE sphere.")]
    public float specialRange = 5.0f;

    [Tooltip("Horizontal knockback applied to enemies hit by a light attack.")]
    public float knockBackForce = 7f;

    [Header("Combo")]
    [Tooltip("Grace period, in seconds. If the player does not chain the next attack within this time, the combo is lost and the next hit starts again at step 1. Measured from the last chained step, so keeping the rhythm keeps the combo alive. This is the only thing that ends a combo on its own.")]
    public float comboResetTime = 1.0f;

    [Tooltip("Minimum seconds between attacks. The attack animation lasts ~0.2s real time, so a much larger value makes the attacks feel unresponsive.")]
    public float attackCooldown = 0.15f;

    [Tooltip("Animator.speed multiplier used while performing light/combo attacks.")]
    public float attackAnimationSpeed = 1.5f;

    [Tooltip("Animator.speed multiplier used while performing the special attack.")]
    public float specialAttackAnimationSpeed = 1.2f;

    [Header("Special Attack")]
    [Tooltip("Cooldown (seconds) of the special attack once used.")]
    public float specialCooldown = 10f;

    [Tooltip("Knockback force applied by the special AOE hit.")]
    public float specialKnockbackForce = 10f;
}
