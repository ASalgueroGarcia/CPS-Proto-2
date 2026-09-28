using System.Collections;
using UnityEngine;

public class FallingTrap : TrapBase
{
    [Header("FALLING TRAP SETTINGS")]
    [SerializeField] private GameObject trapObjectFalling;
    [SerializeField] private GameObject warningShadow;
    [SerializeField] private float fallingTimeDelay = 0.8f;
    [SerializeField] private float cooldown = 6.0f;
    [SerializeField] private float damage = 30.0f; // same damage to player and enemys.
    
    private bool Triggered = false;
    private bool onCooldon = false;

    private void OnTriggerEnter(Collider other)
    {
        if(Triggered ||onCooldon)return;
        if(other.CompareTag("Enemy") || other.CompareTag("Player"))
        {
            // player or enemy detected ? -> starts the delay.
            StartCoroutine(FallingC(other));
        }
    }
    
    private IEnumerator FallingC(Collider other)
    {
        // delay ->
        Triggered = true;
        yield return new WaitForSeconds(fallingTimeDelay);

        // if the player is still in the zone-> damage.
        if (IsTargetInZone(other))
        {
            DamageZone();
        }

        // Cooldown before the fall.
        onCooldon = true;
        yield return new WaitForSeconds(cooldown);
        onCooldon = false;
        Triggered = false;
    }

    private bool IsTargetInZone(Collider other)
    {
        if(other == null)return false;

        // Colls in the zone.
        Collider tz = GetComponent<Collider>();
        if(tz == null) return false;


        // Check if the object collider center (other.bounds.center) is within the bounds ->
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
    public override void TrapActive() {}
    public override void TrapDesactive() {}
    public override void OnPlayerEnter(GameObject player) {}
    public override void OnEnemyEnter(GameObject enemy) {}
}
