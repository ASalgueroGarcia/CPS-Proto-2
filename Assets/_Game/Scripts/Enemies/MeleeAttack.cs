using UnityEngine;

/// <summary>
/// Strategy: chase the player until in range, wind up, then deal melee damage.
/// Simple, reliable, no projectile management. Good for grounded melee enemies.
/// </summary>
public class MeleeAttack : MonoBehaviour, IEnemyAttackStrategy
{
    [Header("Melee Settings")]
    [Tooltip("How far the attack reaches. Must be <= EnemyData.alertRange or the enemy will never reach Executing.")]
    public float attackRange = 2.5f;

    [Tooltip("Extra reach added during the Executing frame (forgiving hit detection).")]
    public float attackLunge = 0.5f;

    [Tooltip("Visual feedback duration when the attack lands.")]
    public float flashDuration = 0.2f;

    // IEnemyAttackStrategy contract ---------------------------------------------------------------

    public float ExecutingDuration => 0f; // Instant attack

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        // Standard chase
        owner.MoveTowards(owner.PlayerTransform.position, owner.data.speed);
    }

    public void OnWindup(Enemy owner)
    {
        // Flash red to warn the player
        owner.FlashColor(Color.red, 0.15f);
    }

    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        // Damage if still in range (with forgiving lunge)
        if (distanceToPlayer <= attackRange + attackLunge)
        {
            if (owner.PlayerHealth != null)
                owner.PlayerHealth.TakeDamage(owner.data.damage);
        }

        owner.FlashColor(Color.red, flashDuration);
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
        // Default: do nothing while catching breath
    }
}
