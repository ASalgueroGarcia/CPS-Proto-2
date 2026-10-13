using UnityEngine;

public class EnemyStraightProjectile : MonoBehaviour
{
    private Transform playerTransform;
    private Health playerHealth;
    private Vector3 direction;
    private float speed;
    private float range;
    private float damage;
    private float hitRadius;
    private float travelled;

    public static void Launch(Vector3 from, Vector3 direction, Enemy owner, float damage, float speed, float range,
        float size, Material material)
    {
        GameObject shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shot.name = "Flower Seed";
        Destroy(shot.GetComponent<Collider>());
        shot.transform.position = from;
        shot.transform.localScale = Vector3.one * size;
        if (material != null) shot.GetComponent<Renderer>().sharedMaterial = material;

        EnemyStraightProjectile projectile = shot.AddComponent<EnemyStraightProjectile>();
        projectile.playerTransform = owner.PlayerTransform;
        projectile.playerHealth = owner.PlayerHealth;
        projectile.direction = direction.normalized;
        projectile.damage = damage;
        projectile.speed = speed;
        projectile.range = range;
        projectile.hitRadius = size * 0.5f + 0.5f;
    }

    private void Update()
    {
        float step = Mathf.Min(speed * Time.deltaTime, range - travelled);

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy"))
        {
            Destroy(gameObject);
            return;
        }

        transform.position += direction * step;
        travelled += step;

        if (playerTransform != null && playerHealth != null)
        {
            Vector3 toPlayer = playerTransform.position + Vector3.up - transform.position;
            if (toPlayer.magnitude <= hitRadius)
            {
                playerHealth.TakeDamage(damage, transform.position);
                Destroy(gameObject);
                return;
            }
        }

        if (travelled >= range)
            Destroy(gameObject);
    }
}
