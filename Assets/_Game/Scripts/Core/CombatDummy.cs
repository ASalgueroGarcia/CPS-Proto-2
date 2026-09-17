using UnityEngine;

public class CombatDummy : MonoBehaviour
{
    [Header("Settings")]
    public Health dummyHealth; // Link Health script here
    public bool counterAttack = false;
    public float counterAttackDelay = 0.5f;
    public float counterDamage = 15f;

    [Header("Visuals")]
    public MeshRenderer dummyRenderer;
    private Color originalColor;
    private Camera mainCam;

    private void OnEnable()
    {
        if (dummyHealth == null) dummyHealth = GetComponent<Health>();

        if (dummyRenderer != null) originalColor = dummyRenderer.material.color;
        mainCam = Camera.main;
        
        if (dummyHealth != null)
        {
            dummyHealth.OnDamageTaken.AddListener(OnDamage);
        }

        if (GetComponent<EnemyUIAutoSetup>() == null)
        {
            gameObject.AddComponent<EnemyUIAutoSetup>();
        }
    }

    private void OnDisable()
    {
        if (dummyHealth != null)
        {
            dummyHealth.OnDamageTaken.RemoveListener(OnDamage);
        }
    }

    private void OnDamage(float damage)
    {
        FlashColor(Color.red);

        if (counterAttack)
        {
            Invoke("HitPlayerBack", counterAttackDelay);
        }
    }

    private void HitPlayerBack()
    {
        // Search for player specifically by Health component
        Collider[] hits = Physics.OverlapSphere(transform.position, 5.0f);
        foreach (var hit in hits)
        {
            Health playerHealth = hit.GetComponent<Health>();
            // Ensure it's the player (by tag or by having PlayerFSM)
            if (playerHealth != null && hit.GetComponent<PlayerFSM>() != null)
            {
                playerHealth.TakeDamage(counterDamage);
                Debug.Log("Dummy counter-attacked Player!");
                break;
            }
        }
    }

    private void FlashColor(Color color)
    {
        if (dummyRenderer != null)
        {
            dummyRenderer.material.color = color;
            Invoke("ResetColor", 0.1f);
        }
    }

    private void ResetColor()
    {
        if (dummyRenderer != null) dummyRenderer.material.color = originalColor;
    }
}