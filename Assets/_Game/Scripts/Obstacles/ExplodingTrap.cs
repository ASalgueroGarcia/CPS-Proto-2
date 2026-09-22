using System.Collections;
using UnityEngine;

public class ExplodingTrap : MonoBehaviour
{
    [Header("DAMAGE SETTINGS")]
    [SerializeField] private float damageToPlayer = 20f;
    [SerializeField] private float damageToEnemy = 20f;

    [Header("EXPLODING TRAP SETTINGS")]
    [SerializeField] private float explosionRadius = 5.0f;
    [SerializeField] private float delayBetweenTrigger = 1.5f;
    [SerializeField] private float knockbackEffect = 10.0f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem explosionEffectRef;
    [SerializeField] private float expTime = 2.0f;
    [SerializeField] private Renderer trapRender;

    [Header("SFX Settings")] 
    [SerializeField] private AudioClip explosionClip;
    
    private bool _isTriggered = false;

    // detect -> player or enemy tag.
    private void OnTriggerEnter(Collider other)
    {
        if (_isTriggered)return;

        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            Vector3 closestPoint = other.ClosestPoint(transform.position);
            if (Vector3.Distance(transform.position, closestPoint) > explosionRadius)
            {
                return;
            }

            StartCoroutine(ExplodeC());
            _isTriggered = true;
        }
    }

    // Coroutine
    IEnumerator ExplodeC()
    {
        if (trapRender != null){
            trapRender.material.color = Color.yellow;
        }

        if (trapRender != null){
            trapRender.material.color = Color.red;
        }

        yield return new WaitForSeconds(delayBetweenTrigger);
        if(explosionEffectRef != null)
        {
            ParticleSystem ef= Instantiate(explosionEffectRef, transform.position, Quaternion.identity);
            ef.Play();
            SoundManager.Instance.PlaySound(explosionClip);
            Destroy(ef.gameObject,expTime);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Vector3 dir = (hits[i].transform.position - transform.position).normalized + Vector3.up * 1.5f;

            if (hits[i].CompareTag("Player"))
            {
                hits[i].GetComponent<Health>()?.TakeDamage(damageToPlayer);
                var playerController = hits[i].GetComponent<PlayerFSM>();
                if (playerController != null)
                {
                    playerController.ApplyKnockback(dir.normalized, knockbackEffect, 0.2f);
                }
            }
            else if (hits[i].CompareTag("Enemy"))
            {
                hits[i].GetComponent<Health>()?.TakeDamage(damageToEnemy);
                Rigidbody rb = hits[i].GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(dir.normalized * knockbackEffect, ForceMode.Impulse);
                }
            }
        }
        Destroy(gameObject);
    }
}