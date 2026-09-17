using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient colorGradient;

    private Camera mainCamera;

    private void Start()
    {
        if (health == null) health = GetComponentInParent<Health>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (healthSlider == null) healthSlider = GetComponentInChildren<Slider>();
        
        if (fillImage == null && healthSlider != null) 
        {
            if (healthSlider.fillRect != null) fillImage = healthSlider.fillRect.GetComponent<Image>();
        }

        if (health != null)
        {
            Initialize(health);
        }
    }

    public void Initialize(Health targetHealth)
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(OnHealthChanged);
        }

        health = targetHealth;
        health.OnHealthChanged.AddListener(OnHealthChanged);
        OnHealthChanged(health.currentHealth, health.maxHealth);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
            UpdateHealthBarVisuals(current, max);
        }
    }

    private void Update()
    {
        // Removed health polling from Update
    }

    private void LateUpdate()
    {
        if (mainCamera == null || !mainCamera.isActiveAndEnabled)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    private void UpdateHealthBarVisuals(float current, float max)
    {
        if (healthSlider == null || max <= 0) return;

        float healthPercent = current / max;
        if (fillImage != null && colorGradient != null)
        {
            fillImage.color = colorGradient.Evaluate(healthPercent);
        }
    }
}
