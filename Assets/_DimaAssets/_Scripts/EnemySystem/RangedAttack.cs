using UnityEngine;

/// <summary>
/// Strategy: maintain distance from the player, aim during windup, then fire a projectile.
/// Good for enemies that prefer not to engage in melee.
/// </summary>
public class RangedAttack : MonoBehaviour, IEnemyAttackStrategy
{
    [Header("Ranged Settings")]
    [Tooltip("Ideal distance the enemy tries to maintain from the player.")]
    public float idealDistance = 15f;

    [Tooltip("Tolerance: if closer than idealDistance - deadzone, back up.")]
    public float distanceDeadzone = 2f;

    [Header("Projectile")]
    [Tooltip("Prefab to instantiate when shooting. Must have a Projectile component.")]
    public GameObject projectilePrefab;

    [Tooltip("Optional spawn point transform. If null, spawns in front of the enemy.")]
    public Transform shootPoint;

    [Tooltip("Launch angle for the projectile arc (degrees).")]
    public float launchAngle = 45f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;

    // IEnemyAttackStrategy contract ---------------------------------------------------------------

    public float ExecutingDuration => 0f; // Instant fire

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        // Keep ideal distance: too close → flee, too far → chase, in zone → stay
        if (distanceToPlayer < idealDistance - distanceDeadzone)
        {
            owner.MoveAwayFrom(owner.PlayerTransform.position, owner.data.speed);
        }
        else if (distanceToPlayer > idealDistance + distanceDeadzone)
        {
            owner.MoveTowards(owner.PlayerTransform.position, owner.data.speed);
        }
        else
        {
            // In the sweet spot — stop moving, face the player
            owner.Agent.ResetPath();
            FacePlayer(owner);
        }
    }

    public void OnWindup(Enemy owner)
    {
        // Face the player while aiming
        FacePlayer(owner);

        // Visual telegraph: flash red to warn
        owner.FlashColor(Color.red, 0.15f);
    }

    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        FacePlayer(owner);
        Shoot(owner);
        owner.FlashColor(Color.red, 0.2f);
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
        // Maintain distance while on cooldown
        if (distanceToPlayer < idealDistance - 5f)
        {
            owner.MoveAwayFrom(owner.PlayerTransform.position, owner.data.speed);
        }
    }

    // --- Internal helpers ------------------------------------------------------------------------

    private void FacePlayer(Enemy owner)
    {
        Vector3 direction = (owner.PlayerTransform.position - owner.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
            owner.transform.forward = direction;
    }

    private void Shoot(Enemy owner)
    {
        Vector3 spawnPos = GetSpawnPosition(owner);
        GameObject projObj;

        if (projectilePrefab != null)
        {
            projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity, owner.transform.parent);
        }
        else
        {
            // Fallback: create a primitive sphere if prefab is missing
            projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projObj.transform.SetParent(owner.transform.parent);
            projObj.transform.position = spawnPos;
            projObj.transform.localScale = Vector3.one * 0.5f;
            projObj.GetComponent<SphereCollider>().isTrigger = true;
            projObj.AddComponent<Projectile>();
        }

        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            Vector3 targetPos = owner.PlayerTransform.position + Vector3.up * 1f;
            proj.Setup(targetPos, owner.data.damage, launchAngle);
        }

        if (shootSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(shootSound);
    }

    private Vector3 GetSpawnPosition(Enemy owner)
    {
        if (shootPoint != null && shootPoint.IsChildOf(owner.transform))
            return shootPoint.position;

        return owner.transform.position + owner.transform.forward * 1.5f + Vector3.up * 1f;
    }
}
