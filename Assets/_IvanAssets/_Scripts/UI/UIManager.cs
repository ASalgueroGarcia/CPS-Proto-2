
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("PAUSE MENU")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private Button inGamePauseButton;

[Header("MAIN MENU")]
    [SerializeField] private Canvas mainMenuCanvas;

    [Header("GAME UI")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI damageText;

    private PlayerStatsManager playerStats;
    private Health playerHealth;
    private PlayerFSM playerFSM;
    private bool isPaused = false;

    private static UIManager _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            gameObject.SetActive(false);
            Destroy(_instance);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(_instance);
    }

    private void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStatsManager>();
        playerHealth = FindFirstObjectByType<Health>();
        playerFSM = FindFirstObjectByType<PlayerFSM>();        
        // Only auto-setup if the user hasn't assigned them in the inspector
        if (playerHealthSlider == null || healthText == null || speedText == null || damageText == null)
        {
            EnsureUIElementsExist();
        }

        if (pausePanel != null){
            pausePanel.SetActive(false);
        }

        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePressed;
        }

        if (playerHealthSlider != null && playerHealth != null)
        {
            playerHealthSlider.maxValue = playerHealth.maxHealth;
            playerHealthSlider.value = playerHealth.currentHealth;
        }

        //SceneController.Instance._onMapLoaded += Resume;
    }

    private void EnsureUIElementsExist()
    {
        // Find or create the main Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MainUICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Configure Canvas Scaler for "Not Pixels" (Scale with Screen Size)
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Create Container for Top UI
        if (playerHealthSlider == null || healthText == null)
        {
            GameObject topUI = new GameObject("PlayerTopUI");
            topUI.transform.SetParent(canvas.transform, false);
            RectTransform topRect = topUI.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0.5f, 1f);
            topRect.anchorMax = new Vector2(0.5f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = new Vector2(0, -20);
            topRect.sizeDelta = new Vector2(400, 100);

            // Health Slider
            if (playerHealthSlider == null)
            {
                GameObject sliderObj = new GameObject("PlayerHealthSlider");
                sliderObj.transform.SetParent(topUI.transform, false);
                playerHealthSlider = sliderObj.AddComponent<Slider>();
                RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
                sliderRect.sizeDelta = new Vector2(350, 30);
                sliderRect.anchoredPosition = new Vector2(0, -10);

                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(sliderObj.transform, false);
                RectTransform bgRect = bg.AddComponent<RectTransform>();
                Image bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0, 0, 0, 0.5f);
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;

                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(sliderObj.transform, false);
                RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;

                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                RectTransform fillRect = fill.AddComponent<RectTransform>();
                Image fillImg = fill.AddComponent<Image>();
                fillImg.color = Color.green;
                playerHealthSlider.fillRect = fillRect;
                playerHealthSlider.fillRect.sizeDelta = Vector2.zero;
            }

            // Health Text
            if (healthText == null)
            {
                GameObject txtObj = new GameObject("HealthText");
                txtObj.transform.SetParent(topUI.transform, false);
                RectTransform txtRect = txtObj.AddComponent<RectTransform>();
                healthText = txtObj.AddComponent<TextMeshProUGUI>();
                healthText.fontSize = 24;
                healthText.alignment = TextAlignmentOptions.Center;
                txtRect.anchoredPosition = new Vector2(0, -45);
            }
        }

        // Speed and Damage Texts
        if (speedText == null)
        {
            GameObject speedObj = new GameObject("SpeedText");
            speedObj.transform.SetParent(canvas.transform, false);
            RectTransform speedRect = speedObj.AddComponent<RectTransform>();
            speedText = speedObj.AddComponent<TextMeshProUGUI>();
            speedText.fontSize = 20;
            speedRect.anchorMin = new Vector2(0, 1);
            speedRect.anchorMax = new Vector2(0, 1);
            speedRect.pivot = new Vector2(0, 1);
            speedRect.anchoredPosition = new Vector2(20, -20);
            speedText.text = "Speed: 0";
        }

        if (damageText == null)
        {
            GameObject dmgObj = new GameObject("DamageText");
            dmgObj.transform.SetParent(canvas.transform, false);
            RectTransform dmgRect = dmgObj.AddComponent<RectTransform>();
            damageText = dmgObj.AddComponent<TextMeshProUGUI>();
            damageText.fontSize = 20;
            dmgRect.anchorMin = new Vector2(0, 1);
            dmgRect.anchorMax = new Vector2(0, 1);
            dmgRect.pivot = new Vector2(0, 1);
            dmgRect.anchoredPosition = new Vector2(20, -50);
            damageText.text = "Damage: 0";
        }
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
        }
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (_isPaused){
            Resume();
        }
        else{
            Pause();
        }
    }

    private void Update()
    {
        if (isPaused) return;

        UpdatePlayerUI();
    }

    private void UpdatePlayerUI()
    {
        if (playerHealth != null)
        {
            if (playerHealthSlider != null)
            {
                playerHealthSlider.maxValue = playerHealth.maxHealth;
                playerHealthSlider.value = playerHealth.currentHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"HP: {(int)playerHealth.currentHealth} / {(int)playerHealth.maxHealth}";
            }
        }

        if (playerFSM != null)
        {
            if (speedText != null)
            {
                speedText.text = $"Speed: {playerFSM.speed:F1}";
            }

            if (damageText != null)
            {
                damageText.text = $"DMG: {playerFSM.weaponBaseDamage:F1}";
            }
        }
    }
    public void StartGame()
    {
        Time.timeScale = 1f;
        mainMenuCanvas.gameObject.SetActive(false);
        SceneManager.LoadScene("_MapScene");
        
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Pause()
    {
        if (pausePanel != null){
            pausePanel.SetActive(true);
        }
        inGamePauseButton.gameObject.SetActive(false);
        Time.timeScale = 0f;
        _isPaused = true;
    }

    public void Resume()
    {
        if (pausePanel != null){
            pausePanel.SetActive(false);
        }
        if (inGamePauseButton != null){
            inGamePauseButton.gameObject.SetActive(true);
        }
        
        Time.timeScale = 1f;
        _isPaused = false;
    }
    
    public void ReturnToMap()
    {
        Time.timeScale = 1f;
        SceneController.Instance.UnloadLevel();
    }
}