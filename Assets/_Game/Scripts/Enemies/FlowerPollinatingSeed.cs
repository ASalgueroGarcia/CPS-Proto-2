using System;
using UnityEngine;

public class FlowerPollinatingSeed : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float arcHeight;
    private float flightTime;
    private float damage;
    private float hitRadius;
    private float elapsed;
    private Transform playerTransform;
    private Health playerHealth;
    private Action<Vector3> onLanded;

    public static void Launch(
        Vector3 start,
        Vector3 target,
        float arcHeight,
        float flightTime,
        float damage,
        float size,
        Material material,
        Enemy owner,
        Action<Vector3> onLanded)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = "Pollinating Seed";
        Destroy(obj.GetComponent<Collider>());
        obj.transform.position = start;
        obj.transform.localScale = Vector3.one * size;
        if (material != null) obj.GetComponent<Renderer>().sharedMaterial = material;

        FlowerPollinatingSeed seed = obj.AddComponent<FlowerPollinatingSeed>();
        seed.startPosition = start;
        seed.targetPosition = target;
        seed.arcHeight = Mathf.Max(0.1f, arcHeight);
        seed.flightTime = Mathf.Max(0.1f, flightTime);
        seed.damage = damage;
        seed.hitRadius = size * 0.5f + 0.5f;
        seed.playerTransform = owner != null ? owner.PlayerTransform : null;
        seed.playerHealth = owner != null ? owner.PlayerHealth : null;
        seed.onLanded = onLanded;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightTime);
        transform.position = FlowerDecisionLogic.EvaluateCurvedSeedPosition(startPosition, targetPosition, arcHeight, t);

        if (playerTransform != null && playerHealth != null)
        {
            Vector3 toPlayer = playerTransform.position + Vector3.up - transform.position;
            if (toPlayer.magnitude <= hitRadius)
            {
                playerHealth.TakeDamage(damage, transform.position);
                onLanded?.Invoke(targetPosition);
                Destroy(gameObject);
                return;
            }
        }

        if (t >= 1f)
        {
            onLanded?.Invoke(targetPosition);
            Destroy(gameObject);
        }
    }
}
