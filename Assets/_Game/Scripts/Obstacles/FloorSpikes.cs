using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorSpikes : MonoBehaviour
{
    [Header("SPIKE TRAP SETTINGS")]
    [SerializeField] private float triggerTime = 2f;
    [SerializeField] private float inactiveTime = 2f;
    [SerializeField] private float damage = 15.0f;
    //[SerializeField] private GameObject spikesVisuals; // VISUALS OF THE SPYKES.
    //[SerializeField] private GameObject warningEffect;
    [SerializeField] private Renderer spikesRenderer; // Renderer para feedback visual

    [Header("SFX Settings")]
    [SerializeField] private AudioClip spikeClip;
    
    // HashSet -> this function will verify if the player or the enemy has already been hit by the spikes THAT TIME, so it won't take damage again until it leaves and re-enters the spikes..
    private readonly HashSet<Health> characterAlreadyHit = new HashSet<Health>();


    private Color _originalColor;
    private bool _isA = false;

    private void Start()
    {
        if (spikesRenderer != null){
            _originalColor = spikesRenderer.material.color;
        }
        StartCoroutine(SpikeC());
    }

private IEnumerator SpikeC()
{
    while (true)
    {
        // WARNING VISUALS BEFORE THE SPIKES ACTIVATION.
        /* if(warningEffect != null)
        {
            spikesVisuals.SetActive(true);
        }*/

        if (spikesRenderer != null){
            spikesRenderer.material.color = Color.yellow;
        }
        yield return new WaitForSeconds(inactiveTime - 0.5f);

        // Warning visual duration
        yield return new WaitForSeconds(0.5f);

        // Reset of the logic.
        characterAlreadyHit.Clear();
        _isA = true;

        if (spikesRenderer != null){
            spikesRenderer.material.color = Color.red;
        }
        if (SoundManager.Instance != null && spikeClip != null){
            SoundManager.Instance.PlaySound(spikeClip);
        }

        yield return new WaitForSeconds(triggerTime);

        _isA = false;

        if (spikesRenderer != null)
            spikesRenderer.material.color = _originalColor;

        // spikesVisuals.SetActive(false);
    }
}

    // OntriggerStay -> checks if the player or the enemy is still on the spikes while is active -> DAMAGE.
    private void OnTriggerStay(Collider other)
    {
        if(!_isA)return;
        
        if (!other.CompareTag("Enemy") && !other.CompareTag("Player")) return;
        var health = other.GetComponentInParent<Health>();
        
        if(health==null)return;
        if(characterAlreadyHit.Add(health))
        {
            health.TakeDamage(damage);
        }
    }
}