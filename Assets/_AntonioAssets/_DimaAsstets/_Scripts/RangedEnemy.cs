using UnityEngine;

public class RangedEnemy : EnemyBase
{
    private enum AttackSubState { Positioning, Attacking, Cooldown }
    private AttackSubState attackState = AttackSubState.Positioning;

    [Header("Ranged Enemy Settings")]
    public float keepDistance = 15f;
    public float attackCooldown = 2f;
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float projectileSpeed = 10f;

    private float timer = 0f;

    protected override void Awake()
    {
        maxHealth = 25f;
        damage = 30f;
        speed = 5f;
        alertRange = 30f;
        base.Awake();
    }

    public override void ChangeState(EnemyState newState)
    {
        base.ChangeState(newState);
        if (newState == EnemyState.Attack)
        {
            attackState = AttackSubState.Positioning;
        }
    }

    protected override void AttackBehavior(float distanceToPlayer)
    {
        if (distanceToPlayer > alertRange * 1.5f)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        switch (attackState)
        {
            case AttackSubState.Positioning:
                if (distanceToPlayer < keepDistance - 2f)
                {
                    MoveAwayFrom(playerTransform.position, speed);
                }
                else if (distanceToPlayer > keepDistance + 2f)
                {
                    MoveTowards(playerTransform.position, speed);
                }
                else
                {
                    attackState = AttackSubState.Attacking;
                    timer = 0.5f; // Windup
                }
                break;

            case AttackSubState.Attacking:
                // Keep looking at player
                Vector3 direction = (playerTransform.position - transform.position).normalized;
                direction.y = 0;
                transform.forward = direction;

                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    Shoot();
                    attackState = AttackSubState.Cooldown;
                    timer = attackCooldown;
                }
                break;

            case AttackSubState.Cooldown:
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    attackState = AttackSubState.Positioning;
                }
                // Try to keep distance while on cooldown
                if (distanceToPlayer < keepDistance - 5f)
                {
                    MoveAwayFrom(playerTransform.position, speed);
                }
                break;
        }
    }

    private void Shoot()
    {
        FlashColor(Color.red, 0.2f);
        GameObject projObj;
        
        Vector3 spawnPos = transform.position + transform.forward * 1.5f + Vector3.up * 1f;
        if (shootPoint != null && shootPoint.IsChildOf(transform))
        {
            spawnPos = shootPoint.position;
        }

        if (projectilePrefab != null)
        {
            projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity, transform.parent);
        }
        else
        {
            // Fallback: create a simple sphere if prefab is missing
            projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projObj.transform.parent = transform.parent;
            projObj.transform.position = spawnPos;
            projObj.transform.localScale = Vector3.one * 0.5f;
            projObj.GetComponent<SphereCollider>().isTrigger = true;
            projObj.AddComponent<Projectile>();
        }

        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
        {
            Vector3 targetPos = playerTransform.position + Vector3.up * 1f;
            // Vector3 direction = (targetPos - spawnPos).normalized;
            proj.Setup(targetPos, damage, 45);    // for now lets make a static angle 45deg
        }
    }
}
