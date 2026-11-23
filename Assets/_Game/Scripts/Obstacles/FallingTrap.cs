using System.Collections;
using UnityEngine;

public class FallingTrap : MonoBehaviour
{
    [Header("FALLING TRAP SETTINGS")]
    [SerializeField] private GameObject trapObjectFalling;
    [SerializeField] private GameObject warningShadow;
    [SerializeField] private float fallingTimeDelay = 0.8f;
    [SerializeField] private float cooldown = 6.0f;
    [SerializeField] private float damage = 30.0f; // same damage to player and enemys.
    [Tooltip("How hard a player caught under the impact is shoved away, in a random direction.")]
    [SerializeField] private float playerPushForce = 10f;
    [SerializeField] private float playerPushDuration = 0.2f;

    [Header("VISUALS")]
    [Tooltip("Height the falling object drops from.")]
    [SerializeField] private float dropHeight = 12f;
    [Tooltip("Size of the floor shadow while nothing is falling, as a fraction of its full size.")]
    [Range(0f, 1f)] [SerializeField] private float idleShadowScale = 0.35f;
    [Tooltip("Seconds the fallen object stays on the floor before it is cleared away.")]
    [SerializeField] private float restTime = 1f;

    private Vector3 fullShadowScale;
    private Vector3 landedPosition;

    private bool triggered = false;
    private bool onCooldown = false;
    private Collider tCollider;

    private void Awake()
    {
        tCollider = GetComponent<Collider>();
        if (warningShadow != null) fullShadowScale = warningShadow.transform.localScale;
        if (trapObjectFalling != null) landedPosition = trapObjectFalling.transform.localPosition;
        ResetVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || onCooldown)
        {
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("Player"))
        {
            StartCoroutine(FallingC());
        }
    }

    private IEnumerator FallingC()
    {
        triggered = true;
        yield return Fall();

        DamageZone();

        onCooldown = true;
        yield return new WaitForSeconds(Mathf.Min(restTime, cooldown));
        ResetVisuals();
        yield return new WaitForSeconds(Mathf.Max(0f, cooldown - restTime));
        onCooldown = false;
        triggered = false;
    }

    // The object drops for the whole warning time while its shadow grows to full size, so the shadow shows how close the impact is.
    private IEnumerator Fall()
    {
        if (trapObjectFalling != null) trapObjectFalling.SetActive(true);

        for (float t = 0f; t < fallingTimeDelay; t += Time.deltaTime)
        {
            float k = t / fallingTimeDelay;
            if (trapObjectFalling != null)
                trapObjectFalling.transform.localPosition = landedPosition + Vector3.up * (dropHeight * (1f - k * k));
            SetShadow(Mathf.Lerp(idleShadowScale, 1f, k));
            yield return null;
        }

        if (trapObjectFalling != null) trapObjectFalling.transform.localPosition = landedPosition;
        SetShadow(1f);
    }

    private void ResetVisuals()
    {
        if (trapObjectFalling != null) trapObjectFalling.SetActive(false);
        SetShadow(idleShadowScale);
    }

    private void SetShadow(float size)
    {
        if (warningShadow == null) return;
        warningShadow.transform.localScale = new Vector3(fullShadowScale.x * size, fullShadowScale.y, fullShadowScale.z * size);
    }

    private void DamageZone()
    {
        if (tCollider == null) return;
        Bounds b = tCollider.bounds;

        Collider[] h = Physics.OverlapBox(b.center, b.extents, Quaternion.identity);

        for (int k = 0; k < h.Length; k++)
        {
            if (!h[k].CompareTag("Player") && !h[k].CompareTag("Enemy"))
                continue;

            Health health = h[k].GetComponentInParent<Health>();
            if (health != null)
                health.TakeDamage(damage);

            Player player = h[k].GetComponentInParent<Player>();
            if (player != null)
            {
                Vector2 away = Random.insideUnitCircle.normalized;
                player.ApplyKnockback(new Vector3(away.x, 0f, away.y), playerPushForce, playerPushDuration);
            }
        }
    }
}
