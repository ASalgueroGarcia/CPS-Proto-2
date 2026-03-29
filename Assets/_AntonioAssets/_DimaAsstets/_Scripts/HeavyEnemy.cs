using UnityEngine;

public class HeavyEnemy : EnemyBase
{
    private enum AttackSubState { Chasing, Attacking, Cooldown }
    private AttackSubState attackState = AttackSubState.Chasing;

    [Header("Heavy Enemy Settings")]
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;
    private float timer = 0f;

    protected override void Awake()
    {
        maxHealth = 80f;
        damage = 30f;
        speed = 2f;
        alertRange = 10f;
        base.Awake();
    }

    public override void ChangeState(EnemyState newState)
    {
        base.ChangeState(newState);
        if (newState == EnemyState.Attack)
        {
            attackState = AttackSubState.Chasing;
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
            case AttackSubState.Chasing:
                if (distanceToPlayer <= attackRange)
                {
                    attackState = AttackSubState.Attacking;
                    timer = 0.5f; // Windup
                }
                else
                {
                    MoveTowards(playerTransform.position, speed);
                }
                break;

            case AttackSubState.Attacking:
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    PerformAttack();
                    attackState = AttackSubState.Cooldown;
                    timer = attackCooldown;
                }
                break;

            case AttackSubState.Cooldown:
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    attackState = AttackSubState.Chasing;
                }
                break;
        }
    }

    private void PerformAttack()
    {
        FlashColor(Color.red, 0.2f);
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= attackRange + 0.5f)
        {
            if (playerHealth != null) playerHealth.TakeDamage(damage);
        }
    }
}
