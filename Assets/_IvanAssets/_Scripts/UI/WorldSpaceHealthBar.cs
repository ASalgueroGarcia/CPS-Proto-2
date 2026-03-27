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

        if (healthSlider != null && health != null)
        {
            healthSlider.maxValue = health.maxHealth;
            healthSlider.value = health.currentHealth;
        }

        UpdateHealthBar();
    }

    private void Update()
    {
        // Billboard effect: Make the UI face the camera
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }

        if (health != null && healthSlider != null)
        {
            healthSlider.value = health.currentHealth;
            UpdateHealthBar();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthSlider == null) return;

        float healthPercent = health.currentHealth / health.maxHealth;
        if (fillImage != null && colorGradient != null)
        {
            fillImage.color = colorGradient.Evaluate(healthPercent);
        }
        
        // Hide if full health (optional, but clean)
        // gameObject.SetActive(healthPercent < 1.0f);
    }
}
