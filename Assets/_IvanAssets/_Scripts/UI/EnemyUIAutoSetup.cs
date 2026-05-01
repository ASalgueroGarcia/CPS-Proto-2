using UnityEngine;
using UnityEngine.UI;
using TMPro;


[RequireComponent(typeof(Health))]
public class EnemyUIAutoSetup : MonoBehaviour
{
    [Header("UI Prefab")]
    [SerializeField] private GameObject healthBarPrefab;

    [SerializeField] private Slider healthSlider;
    
    [Header("Manual Settings (Fallback)")] 
    public Vector3 offset = new Vector3(0, 2.5f, 0);
    public Vector2 canvasSize = new Vector2(200, 50);
    public float scaleFactor = 0.01f;

    private Health enemyHealth;
    private GameObject canvasObj;

    private void Start()
    {
        enemyHealth = GetComponent<Health>();

        // 1. Check if a WorldSpaceHealthBar is already attached (manual setup in Editor)
        WorldSpaceHealthBar existingBar = GetComponentInChildren<WorldSpaceHealthBar>();
        if (existingBar != null)
        {
            existingBar.Initialize(enemyHealth);
            return;
        }

        // 2. If not, try to instantiate from prefab
        if (healthBarPrefab != null)
        {
            CreateUIFromPrefab();
        }
        else
        {
            // 3. Last resort: Create procedural UI
            CreateUIProcedural();
        }
    }

    private void CreateUIFromPrefab()
    {
        canvasObj = Instantiate(healthBarPrefab, this.transform);
        canvasObj.transform.localPosition = offset;
        
        WorldSpaceHealthBar healthBar = canvasObj.GetComponent<WorldSpaceHealthBar>();
        if (healthBar == null) healthBar = canvasObj.GetComponentInChildren<WorldSpaceHealthBar>();
        
        if (healthBar != null)
        {
            healthBar.Initialize(enemyHealth);
        }

        TextMeshProUGUI nameText = canvasObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = gameObject.name.Replace("(Clone)", "");
        }
    }

    private void CreateUIProcedural()
    {
        // ... (Keep the procedural logic as fallback)
        // 1. Create Canvas
        canvasObj = new GameObject("EnemyWorldCanvas");
        canvasObj.transform.SetParent(this.transform);
        canvasObj.transform.localPosition = offset;
        canvasObj.transform.localScale = Vector3.one * scaleFactor;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // This makes it not pixelated:
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10; // High quality text rendering in world space

        // 2. Add WorldSpaceHealthBar script
        WorldSpaceHealthBar healthBar = canvasObj.AddComponent<WorldSpaceHealthBar>();

        // 3. Create Slider Background
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = canvasSize;

        healthSlider = sliderObj.AddComponent<Slider>();

        // 4. Background Image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 5. Fill Area & Fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.red;
        fillRect.sizeDelta = Vector2.zero;

        healthSlider.fillRect = fillRect;
        healthSlider.targetGraphic = fillImage;
        healthSlider.minValue = 0;
        healthSlider.maxValue = enemyHealth.maxHealth;
        healthSlider.value = enemyHealth.currentHealth;
        
        // 6. Name Text (TMP)
        GameObject nameObj = new GameObject("EnemyName");
        nameObj.transform.SetParent(canvasObj.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = gameObject.name.Replace("(Clone)", "");
        nameText.fontSize = 24;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        nameRect.anchoredPosition = new Vector2(0, 35);
        nameRect.sizeDelta = new Vector2(200, 50);

        // 7. Setup the WorldSpaceHealthBar component
        // Since we added it via code, we need to manually assign fields or let it find them
        // We'll update WorldSpaceHealthBar to handle this assignment.
    }
}