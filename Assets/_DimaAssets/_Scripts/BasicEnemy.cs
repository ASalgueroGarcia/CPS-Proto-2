using UnityEngine;

public class BasicEnemy : EnemyBase
{
    private enum AttackSubState { Chasing, Dashing, Cooldown }
    private AttackSubState attackState = AttackSubState.Chasing;

    [Header("Basic Enemy Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 2f;
    public float attackRange = 3f;

    private float timer = 0f;
    private Vector3 dashDirection;
    
    // Dash Visual
    private TrailRenderer dashTrail;

    protected override void Awake()
    {
        maxHealth = 50f;
        damage = 10f;
        speed = 5f;
        alertRange = 10f;
        base.Awake();
        
        dashTrail = GetComponent<TrailRenderer>();
        if (dashTrail == null)
        {
            dashTrail = gameObject.AddComponent<TrailRenderer>();
            dashTrail.time = 0.4f;
            dashTrail.startWidth = 1.2f;
            dashTrail.endWidth = 0.1f;
            dashTrail.material = new Material(Shader.Find("Sprites/Default"));
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(new Color(1f, 0.5f, 0f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            dashTrail.colorGradient = gradient;
            dashTrail.emitting = false;
        }
    }

    public override void ChangeState(EnemyState newState)
    {
        base.ChangeState(newState);
        if (newState == EnemyState.Attack)
        {
            attackState = AttackSubState.Chasing;
        }
        if (dashTrail != null) dashTrail.emitting = false;
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
                    StartDash();
                }
                else
                {
                    MoveTowards(playerTransform.position, speed);
                }
                break;

            case AttackSubState.Dashing:
                timer -= Time.deltaTime;
                agent.Move(dashDirection * dashSpeed * Time.deltaTime);
                
                if (Vector3.Distance(transform.position, playerTransform.position) < 2.0f)
                {
                    playerHealth.TakeDamage(damage);
                    attackState = AttackSubState.Cooldown;
                    timer = dashCooldown;
                    if (dashTrail != null)
                    {
                        dashTrail.Clear();
                        dashTrail.emitting = false;
                    }
                }
                else if (timer <= 0)
                {
                    attackState = AttackSubState.Cooldown;
                    timer = dashCooldown;
                    if (dashTrail != null)
                    {
                        dashTrail.Clear();
                        dashTrail.emitting = false;
                    }
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

    private void StartDash()
    {
        attackState = AttackSubState.Dashing;
        timer = dashDuration;
        dashDirection = (playerTransform.position - transform.position).normalized;
        dashDirection.y = 0;
        FlashColor(Color.red, dashDuration);
        
        if (dashTrail != null) 
        {
            dashTrail.Clear();
            dashTrail.emitting = true;
        }
    }
}