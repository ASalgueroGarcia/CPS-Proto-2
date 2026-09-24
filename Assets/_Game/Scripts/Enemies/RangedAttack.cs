using UnityEngine;
using UnityEngine.Pool;

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

    // Projectile pooling: one pool per RangedAttack component; projectiles stay parented
    // to the room so they outlive the enemy that fired them and die with the room.
    private ObjectPool<GameObject> _projectilePool;

    private GameObject GetProjectile(Enemy owner, Vector3 spawnPos)
    {
        if (_projectilePool == null)
        {
            _projectilePool = new ObjectPool<GameObject>(
                createFunc: CreateProjectile,
                actionOnRelease: pooled => pooled.SetActive(false),
                collectionCheck: true);
        }

        GameObject projectile = _projectilePool.Get();
        projectile.transform.SetParent(owner.transform.parent, false);
        projectile.transform.position = spawnPos;
        projectile.transform.rotation = Quaternion.identity;
        projectile.SetActive(true);
        Phase5Verify.Log($"{owner.name}: projectile reused from pool");
        return projectile;
    }

    private GameObject CreateProjectile()
    {
        GameObject obj = Instantiate(projectilePrefab);
        Projectile proj = obj.GetComponent<Projectile>();
        if (proj != null) proj.BindToPool(_projectilePool);
        Phase5Verify.Log($"projectile pool: created new instance (total now {_projectilePool.CountInactive + 1} inactive capacity)");
        return obj;
    }

    // IEnemyAttackStrategy contract ---------------------------------------------------------------

    public float ExecutingDuration => 0f; // Instant fire

    public void OnApproachTarget(Enemy owner)
    {
        // Keep ideal distance: too close → flee, too far → chase, in zone → stay
        if (owner.DistanceToPlayer < idealDistance - distanceDeadzone)
        {
            owner.MoveAwayFrom(owner.PlayerTransform.position, owner.data.speed);
        }
        else if (owner.DistanceToPlayer > idealDistance + distanceDeadzone)
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

    public void OnExecute(Enemy owner)
    {
        FacePlayer(owner);
        Shoot(owner);
        owner.FlashColor(Color.red, 0.2f);
    }

    public void OnCooldown(Enemy owner)
    {
        // Maintain distance while on cooldown
        if (owner.DistanceToPlayer < idealDistance - 5f)
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
        if (projectilePrefab == null)
        {
            Debug.LogError($"[{owner.name}] No projectile prefab assigned - skipping the attack. Assign one in the Inspector.", this);
            return;
        }

        Vector3 spawnPos = GetSpawnPosition(owner);
        GameObject projObj = GetProjectile(owner, spawnPos);

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
