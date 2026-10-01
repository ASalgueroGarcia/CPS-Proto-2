// LEGACY FIELD (review): dashTriggerRange is unused. Enemy Attack Range controls windup; this attack component remains usable.
using UnityEngine;
public class DashAttack : MonoBehaviour, IEnemyAttackStrategy
{
    [Header("Dash Settings")]
    [Tooltip("How fast the enemy moves during the dash lunge.")]
    public float dashSpeed = 15f;

    [Tooltip("How long the dash execution lasts, in seconds.")]
    public float dashDuration = 0.5f;

    [Tooltip("Legacy dash range. The Enemy component's Attack Range controls windup distance.")]
    public float dashTriggerRange = 3f;

    [Tooltip("How close the enemy must get during the dash to actually deal damage.")]
    public float hitDistance = 2.0f;
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

    public float ApproachDuration => 0f;
    public float ExecutingDuration => dashDuration;
    public bool IsExecutionComplete => false;
    public bool BeginsWindupAtAnyDistance => false;

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        owner.MoveTowards(owner.PlayerTransform.position, owner.Settings.speed);
    }

    public void OnWindup(Enemy owner)
    {
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
        owner.Agent.Move(dashDirection * dashSpeed * Time.deltaTime);
        if (!hasDealtDamage && distanceToPlayer <= hitDistance)
        {
            if (owner.PlayerHealth != null)
                owner.PlayerHealth.TakeDamage(owner.Settings.damage);

            hasDealtDamage = true;
        }
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = false;
        }
    }
}
