using UnityEngine;

/// <summary>
/// Owns the Caterpillar's head sphere collider and routes head hits to the shared Health
/// component. Stays active during Split while non-head body colliders are disabled.
/// </summary>
public class CaterpillarHeadHitbox : MonoBehaviour
{
    [SerializeField] private Health sharedHealth;
    [SerializeField] private SphereCollider hitCollider;

    private CaterpillarAttack ownerAttack;

    public Health SharedHealth => sharedHealth;
    public SphereCollider HitCollider => hitCollider;
    public bool IsActive => hitCollider != null && hitCollider.enabled;

    public void Initialize(Health health, SphereCollider collider, CaterpillarAttack attack = null)
    {
        sharedHealth = health != null ? health : GetComponentInParent<Health>();
        hitCollider = collider != null ? collider : GetComponent<SphereCollider>();
        ownerAttack = attack != null ? attack : GetComponentInParent<CaterpillarAttack>();
    }

    public void SetActive(bool active)
    {
        if (hitCollider == null)
            hitCollider = GetComponent<SphereCollider>();

        if (hitCollider != null)
            hitCollider.enabled = active;
    }

    /// <summary>Applies damage to the shared Health when the head hitbox is active.</summary>
    public bool TryApplyDamage(float amount, Vector3 knockbackSource = default, float knockbackForce = 0f)
    {
        if (amount <= 0f || !IsActive)
            return false;

        if (ownerAttack != null)
            return ownerAttack.TryApplyPieceDamage(amount, isHeadPiece: true, knockbackSource, knockbackForce);

        if (sharedHealth == null)
            sharedHealth = GetComponentInParent<Health>();

        if (sharedHealth == null || sharedHealth.isInvulnerable)
            return false;

        sharedHealth.TakeDamage(amount, knockbackSource, knockbackForce);
        return true;
    }
}
