using System.Collections;
using UnityEngine;

public class FallingTrap : MonoBehaviour
{
    [Header("FALLING TRAP SETTINGS")]
    [SerializeField] private GameObject trapObjectFalling;
    //[SerializeField] private GameObject warningShadow;
    [SerializeField] private float fallingTimeDelay = 0.8f;
    [SerializeField] private float fallDurationTime = 0.25f;

    [SerializeField] private float cooldown = 6.0f;
    [SerializeField] private float damage = 30.0f; // same damage to player and enemys.
    
    private bool triggered = false;
    private bool onCooldown = false;
    private Vector3 trapStartPosition; // 4 the position of the trap before fall. 
    private Collider tCollider;


    private void Awake()
    {
        tCollider = GetComponent<Collider>();
        trapStartPosition = trapObjectFalling.transform.position;
        trapObjectFalling.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(triggered ||onCooldown){
            return;
        }

        if(other.CompareTag("Enemy") ||other.CompareTag("Player"))
        {
            // player or enemy detected ? -> starts the delay.
            StartCoroutine(FallingC(other));
        }
    }
    
    private IEnumerator FallingC(Collider other)
    {
        // delay ->
        triggered = true;
        yield return new WaitForSeconds(fallingTimeDelay);

        // if the player is still in the zone-> damage.
        DamageZone();

        if (trapObjectFalling != null){
            trapObjectFalling.SetActive(true);
        }

        onCooldown = true;

        yield return new WaitForSeconds(cooldown);
        trapObjectFalling.SetActive(false);
        onCooldown = false;
        triggered = false;
    }

    private void DamageZone(){
        
        if(tCollider==null)return;
        Bounds b = tCollider.bounds;

        // player or enemy in the box area ? -> damage.
        Collider[] h = Physics.OverlapBox(b.center, b.extents, Quaternion.identity);
        
        for (int k = 0; k < h.Length; k++)
        {
            if (!h[k].CompareTag("Player") &&
                !h[k].CompareTag("Enemy"))
                continue;

            Health health = h[k].GetComponentInParent<Health>();

            if (health != null)
                health.TakeDamage(damage);
        }
    }
}
