using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private Rigidbody rb;

    public void Setup(Vector3 targetPos, float dmg, float angle)
    {
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

        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerFSM>() != null)
        {
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth != null) playerHealth.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Default") || other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}