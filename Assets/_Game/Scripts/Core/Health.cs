using System.Collections;
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
    public UnityEvent<float, float> OnHealthChanged;
    public UnityEvent<float> OnDamageTaken;
    public UnityEvent OnDeath;

    [Header("Settings")]
    public bool autoResetOnDeath = false;
    public bool isInvulnerable = false;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer targetRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private EnemyVisualFeedback enemyVisualFeedback;

    [Header("Fall Settings")]
    [SerializeField] private bool checkFall = true;
    [SerializeField] private float fallThreshold = -5f;
    [SerializeField] private float checkInterval = 0.5f;

    [Header("SFX")] 
    [SerializeField] private AudioClip hitImpactClip;
    [SerializeField] private AudioClip deathClip;

    private bool isDying = false;

    private void Start()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        enemyVisualFeedback = GetComponent<EnemyVisualFeedback>();
        if (enemyVisualFeedback == null && targetRenderer != null)
            originalColor = targetRenderer.material.color;

        _currentHealth = _maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (checkFall)
        {
            InvokeRepeating(nameof(CheckFall), Random.Range(0f, checkInterval), checkInterval);
        }
    }

    private void CheckFall()
    {
        if (isDying) return;

        if (transform.position.y < fallThreshold)
        {
            Die();
        }
    }

    public void TakeDamage(float amount, Vector3 knockbackSource = default, float knockbackForce = 0f)
    {
        if (isInvulnerable || isDying) return;

        SetHealth(_currentHealth - amount);
        
        OnDamageTaken?.Invoke(amount);
        
        TriggerHitFlash(new Color(1f, 0.5f, 0f));
        if (knockbackForce > 0 && knockbackSource != default)
        {
            ApplyKnockback(knockbackSource, knockbackForce);
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
        
        if (SoundManager.Instance != null && hitImpactClip != null) SoundManager.Instance.PlaySound(hitImpactClip);
    }

    private void TriggerHitFlash(Color flashColor)
    {
        if (enemyVisualFeedback == null) enemyVisualFeedback = GetComponent<EnemyVisualFeedback>();
        if (enemyVisualFeedback != null)
        {
            enemyVisualFeedback.ShowHitFlash();
            return;
        }

        if (targetRenderer == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashCoroutine(flashColor));
    }

    private IEnumerator FlashCoroutine(Color flashColor)
    {
        targetRenderer.material.color = flashColor;
        yield return new WaitForSeconds(0.15f);
        targetRenderer.material.color = originalColor;
        flashCoroutine = null;
    }

    private void ApplyKnockback(Vector3 source, float force)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = (transform.position - source).normalized;
            direction.y = 0;
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
        else
        {
            Vector3 direction = (transform.position - source).normalized;
            StartCoroutine(SimpleKnockbackCoroutine(direction, force * 0.1f));
        }
    }

    private IEnumerator SimpleKnockbackCoroutine(Vector3 direction, float distance)
    {
        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            transform.position += direction * (distance * (1f - (elapsed / duration)));
            elapsed += Time.deltaTime;
            yield return null;
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
        if (isDying) return;
        isDying = true;

        CancelInvoke(nameof(CheckFall));

        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} has DIED!");
        
        SoundManager.Instance.PlaySound(deathClip);
        
        Destroy(gameObject, 0.1f);
    }

    public void ResetHealth()
    {
        SetHealth(_maxHealth);
    }

    public void ResetAllHealthsInScene()
    {
        Health[] allHealths = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
        foreach (Health h in allHealths) h.ResetHealth();
    }
}
