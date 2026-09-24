using UnityEngine;
using UnityEngine.Pool;

public class Projectile : MonoBehaviour
{
    private float damage;
    private Rigidbody rb;

    [Header("Pooling")]
    [Tooltip("Pooled lifetime: Despawn() hands the projectile back to its pool instead of Destroying it.")]
    public bool pooledDespawn = false;
    private ObjectPool<GameObject> _owningPool;

    [Header("Explosion Settings")]
    [SerializeField] private bool isExplosive = true;
    [SerializeField] private float explosionRadius = 3.5f;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float explosionDuration = 2f;
    [SerializeField] private float knockbackForce = 10f;

    /// <summary>
    /// Called once by the owning pool (RangedAttack). Setup() itself is the full
    /// reset: it re-caches the rigidbody, zeroes/relaunches velocity and restarts
    /// the lifetime countdown, so nothing extra is needed on reuse.
    /// </summary>
    public void BindToPool(ObjectPool<GameObject> pool)
    {
        _owningPool = pool;
        if (pool != null) pooledDespawn = true;
    }

    private void Despawn()
    {
        CancelInvoke(nameof(Despawn));
        Phase5Verify.Log($"projectile despawned (pooled={pooledDespawn})");
        if (pooledDespawn && _owningPool != null) _owningPool.Release(gameObject);
        else Destroy(gameObject);
    }

    public void Setup(Vector3 targetPos, float dmg, float angle)
    {
        CancelInvoke(nameof(Despawn));
        damage = dmg;
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;

        // 1. Get the distances
        Vector3 displacement = targetPos - transform.position;
        float deltaY = displacement.y; // Height difference
        
        Vector3 planarTarget = new Vector3(targetPos.x, 0, targetPos.z);
        Vector3 planarPosition = new Vector3(transform.position.x, 0, transform.position.z);
        float distanceX = Vector3.Distance(planarPosition, planarTarget);

        // 2. Physics Math: Solve for Speed (v)
        // Formula: v^2 = (g * x^2) / (2 * cos^2(theta) * (x * tan(theta) - y))
        float g = Mathf.Abs(Physics.gravity.y);
        float theta = angle * Mathf.Deg2Rad; // Convert angle to radians
        
        // Calculate the denominator of the projectile formula
        float cosTheta = Mathf.Cos(theta);
        float denominator = 2 * (cosTheta * cosTheta) * (distanceX * Mathf.Tan(theta) - deltaY);

        // Safety check: if denominator is <= 0, the target is physically impossible to hit at this angle
        if (denominator <= 0)
        {
            Debug.LogWarning("Target is unreachable at this angle! Defaulting to time-based launch.");
            // Fallback: Just use a flat speed if the math fails
            rb.linearVelocity = (planarTarget - planarPosition).normalized * 15f + Vector3.up * 10f;
            return;
        }

        float speedSquared = (g * distanceX * distanceX) / denominator;
        float speed = Mathf.Sqrt(speedSquared);

        // 3. Construct the Velocity Vector
        Vector3 horizontalDirection = (planarTarget - planarPosition).normalized;
        Vector3 launchVelocity = horizontalDirection * (speed * Mathf.Cos(theta));
        launchVelocity.y = speed * Mathf.Sin(theta);

        // Apply to Rigidbody
        rb.linearVelocity = launchVelocity;

        // Make the projectile face the direction of travel
        transform.forward = launchVelocity.normalized;

        Invoke(nameof(Despawn), 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isExplosive)
        {
            Explode();
        }
        else if (other.CompareTag("Player") || other.GetComponent<PlayerFSM>() != null)
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null) playerHealth.TakeDamage(damage);
            Despawn();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Default") || other.CompareTag("Ground"))
        {
            Despawn();
        }
    }

    private void Explode()
    {
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, explosionDuration);
        }

        // Players take the full hit, enemies half of it (self-splash preserved).
        AOEDamage.Burst(transform.position, explosionRadius, damage, damage * 0.5f, knockbackForce, gameObject);

        Despawn();
    }

    private void OnDrawGizmosSelected()
    {
        if (isExplosive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}