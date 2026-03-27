using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    [Header("Events")]
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent OnDeath;

    [Header("Settings")]
    public bool autoResetOnDeath = false;
    public bool isInvulnerable = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isInvulnerable) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnDamageTaken?.Invoke(amount);
        
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} has DIED!");
        Destroy(gameObject, 0.1f);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        Debug.Log($"{gameObject.name} health reset.");
    }

    // A helper method to reset all health components in the scene
    public void ResetAllHealthsInScene()
    {
        Health[] allHealths = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
        foreach (Health h in allHealths)
        {
            h.ResetHealth();
        }
        Debug.Log("<color=green>All Healths Reset!</color>");
    }
}