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
    private bool onCooldon = false;

    private void Awake()
    {
        if (warningShadow != null) fullShadowScale = warningShadow.transform.localScale;
        if (trapObjectFalling != null) landedPosition = trapObjectFalling.transform.localPosition;
        ResetVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(triggered ||onCooldon)return;
        if(other.CompareTag("Enemy") || other.CompareTag("Player"))
        {
            // player or enemy detected ? -> starts the delay.
            StartCoroutine(FallingC(other));
        }
    }
    
    private IEnumerator FallingC(Collider other)
    {
        // delay ->
        triggered = true;
        yield return Fall();

        // if the player is still in the zone-> damage.
        if (IsTargetInZone(other))
        {
            DamageZone();
        }

        // Cooldown before the fall.
        onCooldon = true;
        yield return new WaitForSeconds(Mathf.Min(restTime, cooldown));
        ResetVisuals();
        yield return new WaitForSeconds(Mathf.Max(0f, cooldown - restTime));
        onCooldon = false;
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

    private bool IsTargetInZone(Collider other)
    {
        if(other == null)return false;

        // Colls in the zone.
        Collider tz = GetComponent<Collider>();
        if(tz == null){
            return false;
        }
        return tz.bounds.Contains(other.bounds.center);
    }

    private void DamageZone(){

        // player or enemy in the box area ? -> damage.
        Collider[] h = Physics.OverlapBox(transform.position, GetComponent<Collider>().bounds.extents, transform.rotation);

        for (int k = 0; k < h.Length;k++)
        {
            if (h[k].CompareTag("Player")|| h[k].CompareTag("Enemy"))
            {
                h[k].GetComponent<Health>()?.TakeDamage(damage);
            }
        }
    }
}
