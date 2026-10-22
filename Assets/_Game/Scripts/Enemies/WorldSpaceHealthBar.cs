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
    private Vector3 followOffset;
    private Renderer bodyRenderer;
    private float heightPadding;
    private float bodyHeight;

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
        if (healthSlider == null) healthSlider = GetComponentInChildren<Slider>(true);
        if (fillImage == null && healthSlider != null && healthSlider.fillRect != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();

        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(OnHealthChanged);
        }

        if (health != targetHealth)
            followOffset = transform.position - targetHealth.transform.position;
        health = targetHealth;
        health.OnHealthChanged.AddListener(OnHealthChanged);
        OnHealthChanged(health.currentHealth, health.maxHealth);
    }

    public void SetBodyAnchor(Renderer body, float padding)
    {
        bodyRenderer = body;
        heightPadding = padding;
        bodyHeight = 0f;
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (bodyRenderer == null)
        {
            transform.position = health.transform.position + followOffset;
            return;
        }
        Bounds bounds = bodyRenderer.localBounds;
        Matrix4x4 matrix = bodyRenderer.transform.localToWorldMatrix;
        Vector3 center = bodyRenderer.transform.TransformPoint(bounds.center);
        float verticalExtent = Mathf.Abs(matrix.m10) * bounds.extents.x
            + Mathf.Abs(matrix.m11) * bounds.extents.y
            + Mathf.Abs(matrix.m12) * bounds.extents.z;
        float height = center.y + verticalExtent - health.transform.position.y;
        bodyHeight = Mathf.Max(bodyHeight, height);
        transform.position = health.transform.position + Vector3.up * (bodyHeight + heightPadding);
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

    private void LateUpdate()
    {
        if (health == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePosition();

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
