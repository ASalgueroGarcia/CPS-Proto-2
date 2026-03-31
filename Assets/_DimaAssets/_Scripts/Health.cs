using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth = 100f;

    public float maxHealth { get => _maxHealth; set => SetMaxHealth(value); }
    public float currentHealth { get => _currentHealth; set => SetHealth(value); }
    
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged; // (current, max)
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent OnDeath;

    [Header("Settings")]
    public bool autoResetOnDeath = false;
    public bool isInvulnerable = false;

    private void Start()
    {
        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isInvulnerable) return;

        SetHealth(_currentHealth - amount);
        
        OnDamageTaken?.Invoke(amount);
        
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void SetHealth(float amount)
    {
        _currentHealth = Mathf.Clamp(amount, 0, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void SetMaxHealth(float amount)
    {
        _maxHealth = amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} has DIED!");
        Destroy(gameObject, 0.1f);
    }

    public void ResetHealth()
    {
        SetHealth(_maxHealth);
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