using System.Collections;
using UnityEngine;

public class FloorSpikes : TrapBase
{
    [Header("SPIKE TRAP SETTINGS")]
    [SerializeField] private float triggerTime = 2f;
    [SerializeField] private float inactiveTime = 2f;
    [SerializeField] private float damage = 15.0f;
    //[SerializeField] private GameObject spikesVisuals; // VISUALS OF THE SPYKES.
    //[SerializeField] private GameObject warningEffect;
    [SerializeField] private Renderer spikesRenderer; // Renderer para feedback visual
    private Color originalColor;

    private bool isA = false;

    private void Start()
    {
        if (spikesRenderer != null)
            originalColor = spikesRenderer.material.color;
        StartCoroutine(SpikeC());
    }

    private IEnumerator SpikeC()
    {
        while (true)
        {
            // WARKNING VISUALS BEFORE THE SPIKES ACTIVATION.
            /* if(warningEffect != null)
            {
                spikesVisuals.SetActive(true);
            }*/
            if (spikesRenderer != null)
                spikesRenderer.material.color = Color.yellow;
            yield return new WaitForSeconds(0.5f); // Duración del aviso visual

            isA = true;
            if (spikesRenderer != null)
                spikesRenderer.material.color = Color.red;
            yield return new WaitForSeconds(triggerTime); // Tiempo activas

            isA = false;
            if (spikesRenderer != null)
                spikesRenderer.material.color = originalColor;
            // spikesVisuals.SetActive(false);
            yield return new WaitForSeconds(inactiveTime); // inactiveTime -> 2secs and then, repeat the cycle.
        }
    }

    // OntriggerStay -> checks if the player or the enemy is still on the spikes while is active -> DAMAGE.
    private void OnTriggerStay(Collider other)
    {
        if(!isA)return;

        if(other.CompareTag("Enemy") || other.CompareTag("Player"))
        {
            // health comp of the -> player or enemy.
            Health health = other.GetComponent<Health>(); 
            if(health==null)return;
            health.TakeDamage(damage);
        }
    }
    public override void TrapActive() {}
    public override void TrapDesactive() {}
    public override void OnPlayerEnter(GameObject player) {}
    public override void OnEnemyEnter(GameObject enemy) {}
}

