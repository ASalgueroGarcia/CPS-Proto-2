using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single implementation of the sphere-burst damage pattern shared by the player's
/// special attack and projectile explosions: resolve every Health inside the sphere
/// (on the collider or any parent), classify the victim by its root tag, and route
/// damage plus knockback through Health.TakeDamage.
/// ExplodingTrap deliberately keeps its bespoke burst (lifted direction, direct
/// rigidbody forces) - that is trap physics, not the same pattern.
/// </summary>
public static class AOEDamage
{
    /// <summary>
    /// Damages every Health inside the sphere once (deduped across multi-collider
    /// victims). Players take playerDamage, enemies take enemyDamage; victims whose
    /// slot is 0 are skipped. Knockback is applied away from origin via
    /// Health.TakeDamage. Returns how many Health components took damage.
    /// </summary>
    public static int Burst(Vector3 origin, float radius, float playerDamage, float enemyDamage, float knockbackForce, GameObject exclude = null)
    {
        int hitCount = 0;
        Collider[] hits = Physics.OverlapSphere(origin, radius);
        HashSet<Health> damaged = new HashSet<Health>();

        foreach (Collider hit in hits)
        {
            Health health = hit.GetComponentInParent<Health>();
            if (health == null || damaged.Contains(health)) continue;
            if (exclude != null && health.gameObject == exclude) continue;

            float damage;
            if (health.CompareTag("Player")) damage = playerDamage;
            else if (health.CompareTag("Enemy")) damage = enemyDamage;
            else continue;

            if (damage <= 0f) continue;

            health.TakeDamage(damage, origin, knockbackForce);
            damaged.Add(health);
            hitCount++;
        }

        Phase5Verify.Log($"AOE burst r={radius} -> {hitCount} victim(s) (player {playerDamage:0.#}, enemy {enemyDamage:0})");
        return hitCount;
    }
}
