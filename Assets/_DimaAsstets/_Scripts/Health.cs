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

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
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
        
        // For prototype testing: Auto-respawn/reset health
        Invoke("ResetHealth", 1.0f);
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