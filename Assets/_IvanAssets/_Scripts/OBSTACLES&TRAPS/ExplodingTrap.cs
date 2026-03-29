using System;
using System.Collections;
using UnityEngine;

public class ExplodingTrap : TrapBase
{
    [Header("EXPLODING TRAP SETTINGS")]
    [SerializeField] private float explosionRadius = 5.0f;
    [SerializeField] private float delayBetweenTrigger = 1.5f;
    [SerializeField] private float knockbackEffect = 10.0f;
    private bool isTriggered = false;

    [SerializeField] private Renderer trapRender;

    // detect -> player or enemy tag.
    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered)return;

        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            StartCoroutine(ExplodeC());
            isTriggered = true;
        }
    }

    // Coroutine
    IEnumerator ExplodeC()
    {
        if (trapRender != null){
            trapRender.material.color = Color.yellow;
                Debug.Log("Color cambiado a amarillo");
        }

        if (trapRender != null){
            trapRender.material.color = Color.red;
                Debug.Log("Color cambiado a rojo");
        }

        yield return new WaitForSeconds(delayBetweenTrigger);

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

    public override void TrapActive() {}
    public override void TrapDesactive() {}
    public override void OnPlayerEnter(GameObject player) {}
    public override void OnEnemyEnter(GameObject enemy) {}
}