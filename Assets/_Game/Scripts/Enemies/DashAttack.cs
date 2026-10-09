using UnityEngine;

/// <summary>
/// Strategy: dash at high speed toward the player during windup/execution, dealing damage on contact.
/// The dash spans multiple frames (ExecutingDuration > 0), so OnExecute is called repeatedly.
/// </summary>
public class DashAttack : MonoBehaviour, IEnemyAttackStrategy
{
    [Header("Dash Settings")]
    [Tooltip("How fast the enemy moves during the dash lunge.")]
    public float dashSpeed = 15f;

    [Tooltip("How long the dash lasts. MUST match or be less than windup+execute time in EnemyData, or the SM will cut it short.")]
    public float dashDuration = 0.5f;

    [Tooltip("Distance at which the enemy starts the dash. Should equal or be slightly larger than EnemyData.attackRange.")]
    public float dashTriggerRange = 3f;

    [Tooltip("How close the enemy must get during the dash to actually deal damage.")]
    public float hitDistance = 2.0f;

    // Runtime
    private Vector3 dashDirection;
    private TrailRenderer dashTrail;
    private bool hasDealtDamage;

    private void Awake()
    {
        SetupDashTrail();
    }

    private void SetupDashTrail()
    {
        dashTrail = GetComponent<TrailRenderer>();
        if (dashTrail != null) return;

        dashTrail = gameObject.AddComponent<TrailRenderer>();
        dashTrail.time = 0.4f;
        dashTrail.startWidth = 1.2f;
        dashTrail.endWidth = 0.1f;
        dashTrail.material = new Material(Shader.Find("Sprites/Default"));

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new(Color.red, 0.0f),
                new(new Color(1f, 0.5f, 0f), 1.0f)
            },
            new GradientAlphaKey[]
            {
                new(0.8f, 0.0f),
                new(0.0f, 1.0f)
            }
        );
        dashTrail.colorGradient = gradient;
        dashTrail.emitting = false;
    }

    // IEnemyAttackStrategy contract ---------------------------------------------------------------

    public float ExecutingDuration => dashDuration;

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        // Move toward player until in dash range
        owner.MoveTowards(owner.PlayerTransform.position, owner.data.speed);
    }

    public void OnWindup(Enemy owner)
    {
        // Lock in direction and start the visual
        dashDirection = (owner.PlayerTransform.position - owner.transform.position).normalized;
        dashDirection.y = 0;

        hasDealtDamage = false;

        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = true;
        }
    }

    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        // Move manually (ignoring NavMesh pathing) for the dash
        owner.Agent.Move(dashDirection * dashSpeed * Time.deltaTime);

        // Damage on first contact within hit range
        if (!hasDealtDamage && distanceToPlayer <= hitDistance)
        {
            if (owner.PlayerHealth != null)
                owner.PlayerHealth.TakeDamage(owner.data.damage);

            hasDealtDamage = true;
        }
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
        // Clean up trail
        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = false;
        }
    }
}
