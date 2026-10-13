using UnityEngine;
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

    public float ApproachDuration => 0f;
    public float ExecutingDuration => 0f;
    public bool IsExecutionComplete => false;
    public bool BeginsWindupAtAnyDistance => false;

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        if (distanceToPlayer < idealDistance - distanceDeadzone)
        {
            owner.MoveAwayFrom(owner.PlayerTransform.position, owner.Settings.speed);
        }
        else if (distanceToPlayer > idealDistance + distanceDeadzone)
        {
            owner.MoveTowards(owner.PlayerTransform.position, owner.Settings.speed);
        }
        else
        {
            owner.Agent.ResetPath();
            FacePlayer(owner);
        }
    }

    public void OnWindup(Enemy owner)
    {
        FacePlayer(owner);
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
        if (distanceToPlayer < idealDistance - 5f)
        {
            owner.MoveAwayFrom(owner.PlayerTransform.position, owner.Settings.speed);
        }
    }

    private void FacePlayer(Enemy owner)
    {
        Vector3 direction = (owner.PlayerTransform.position - owner.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
            owner.transform.forward = direction;
    }

    private void Shoot(Enemy owner)
    {
        Vector3 spawnPosition = GetSpawnPosition(owner);
        GameObject projectileObject;

        if (projectilePrefab != null)
        {
            projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity, owner.transform.parent);
        }
        else
        {
            projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.transform.SetParent(owner.transform.parent);
            projectileObject.transform.position = spawnPosition;
            projectileObject.transform.localScale = Vector3.one * 0.5f;
            projectileObject.GetComponent<SphereCollider>().isTrigger = true;
            projectileObject.AddComponent<Projectile>();
        }

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile != null)
        {
            Vector3 targetPosition = owner.PlayerTransform.position + Vector3.up * 1f;
            projectile.Setup(targetPosition, owner.Settings.damage, launchAngle);
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
