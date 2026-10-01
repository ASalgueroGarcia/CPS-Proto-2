using UnityEngine;
public class MeleeAttack : MonoBehaviour, IEnemyAttackStrategy
{
    [Header("Melee Settings")]
    [Tooltip("Damage reach. The Enemy component's Attack Range controls when windup starts.")]
    public float attackRange = 2.5f;

    [Tooltip("Extra reach added during the Executing frame (forgiving hit detection).")]
    public float attackLunge = 0.5f;

    [Tooltip("Visual feedback duration when the attack lands.")]
    public float flashDuration = 0.2f;

    public float ApproachDuration => 0f;
    public float ExecutingDuration => 0f;
    public bool IsExecutionComplete => false;
    public bool BeginsWindupAtAnyDistance => false;

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        owner.MoveTowards(owner.PlayerTransform.position, owner.Settings.speed);
    }

    public void OnWindup(Enemy owner)
    {
        owner.FlashColor(Color.red, 0.15f);
    }

    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        if (distanceToPlayer <= attackRange + attackLunge)
        {
            if (owner.PlayerHealth != null)
                owner.PlayerHealth.TakeDamage(owner.Settings.damage);
        }

        owner.FlashColor(Color.red, flashDuration);
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
    }
}
