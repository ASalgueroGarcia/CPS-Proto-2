using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("PAUSE MENU")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private Button inGamePauseButton;
    [SerializeField] private Button returnToMapBtn;

    [Header("MAIN MENU")]
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private GameObject eolCanvas;
    [SerializeField] private GameObject deathCanvas;

    [Header("GAME UI")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI damageText;

    private bool _isPaused = false;
    private Health _playerHealth;
    private PlayerStatsManager _playerStats;
    private PlayerFSM _playerFSM;
    private static UIManager _instance;
    private GameObject _playerUICanvasObj;

    public static UIManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            gameObject.SetActive(false);
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Hide Player UI in Main Menu and Map Scene
        if (_playerUICanvasObj != null)
        {
            _playerUICanvasObj.SetActive(scene.name != "MainMenu" && scene.name != "_MapScene");
        }

        // Refresh references when a new scene is loaded
        _playerStats = FindFirstObjectByType<PlayerStatsManager>();
        _playerHealth = FindFirstObjectByType<Health>();
        _playerFSM = FindFirstObjectByType<PlayerFSM>();
    }

    private void Start()
    {
        _playerStats = FindFirstObjectByType<PlayerStatsManager>();
        _playerHealth = FindFirstObjectByType<Health>();
        _playerFSM = FindFirstObjectByType<PlayerFSM>();

        if (playerHealthSlider == null || healthText == null || speedText == null || damageText == null)
        {
            EnsureUIElementsExist();
        }

        if (pausePanel != null){
            pausePanel.SetActive(false);
        }

        if (eolCanvas == null)
        {
            Transform[] trs = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in trs)
            {
                if (t.name == "EoLCanvas" || t.name == "eolCanvas")
                {
                    eolCanvas = t.gameObject;
                    break;
                }
            }
        }

        if (eolCanvas != null)
        {
            eolCanvas.SetActive(false);
        }

        if (deathCanvas == null)
        {
            Transform[] trs = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in trs)
            {
                if (t.name == "deathCanvas" || t.name == "DeathCanvas")
                {
                    deathCanvas = t.gameObject;
                    break;
                }
            }
        }

        if (deathCanvas != null)
        {
            deathCanvas.SetActive(false);
        }

        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePressed;
        }

        if (playerHealthSlider != null && _playerHealth != null)
        {
            playerHealthSlider.maxValue = _playerHealth.maxHealth;
            playerHealthSlider.value = _playerHealth.currentHealth;
        }

        // Initial visibility check
        if (_playerUICanvasObj != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            _playerUICanvasObj.SetActive(currentScene != "MainMenu" && currentScene != "_MapScene");
        }
    }

    private void EnsureUIElementsExist()
    {
        Canvas canvas = null;
        
        Transform playerUITransform = transform.Find("PlayerUICanvas");
        if (playerUITransform != null)
        {
            _playerUICanvasObj = playerUITransform.gameObject;
            canvas = _playerUICanvasObj.GetComponent<Canvas>();
        }
        else
        {
            _playerUICanvasObj = new GameObject("PlayerUICanvas");
            _playerUICanvasObj.transform.SetParent(this.transform, false);
            canvas = _playerUICanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; 
            _playerUICanvasObj.AddComponent<CanvasScaler>();
            _playerUICanvasObj.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

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

    public void ShowEoLCanvas()
    {
        if (eolCanvas != null)
        {
            eolCanvas.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable(); 
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
        if (_isPaused) return;

        UpdatePlayerUI();
    }

    private void UpdatePlayerUI()
    {
        if (_playerHealth != null)
        {
            if (playerHealthSlider != null)
            {
                playerHealthSlider.maxValue = _playerHealth.maxHealth;
                playerHealthSlider.value = _playerHealth.currentHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"HP: {(int)_playerHealth.currentHealth} / {(int)_playerHealth.maxHealth}";
            }
        }
        else
        {
            _playerHealth = FindFirstObjectByType<Health>();
            _playerFSM = FindFirstObjectByType<PlayerFSM>();
            _playerStats = FindFirstObjectByType<PlayerStatsManager>();
        }

        if (_playerFSM != null)
        {
            if (speedText != null)
            {
                speedText.text = $"Speed: {_playerFSM.speed:F1}";
            }

            if (damageText != null)
            {
                damageText.text = $"DMG: {_playerFSM.weaponBaseDamage:F1}";
            }
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        PlayerFSM.IsPaused = false;

        if (mainMenuCanvas != null) mainMenuCanvas.gameObject.SetActive(false);
        
        // Reset stats if possible
        if (_playerStats == null) _playerStats = FindFirstObjectByType<PlayerStatsManager>();
        if (_playerStats != null) _playerStats.ResetAllThePlayerStats();
        
        SceneManager.LoadScene("_MapScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Pause()
    {
        if (returnToMapBtn != null) returnToMapBtn.gameObject.SetActive(SceneManager.GetActiveScene().name != "MainMenu");

        if (pausePanel != null){
            pausePanel.SetActive(true);
        }
        if (inGamePauseButton != null) inGamePauseButton.gameObject.SetActive(false);
        Time.timeScale = 0f;
        _isPaused = true;
        PlayerFSM.IsPaused = _isPaused;
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
        PlayerFSM.IsPaused = _isPaused;
    }
    
    public void ReturnToMap()
    {
        Time.timeScale = 1f;
        if (SceneController.Instance != null) SceneController.Instance.UnloadLevel();
    }

    public void ShowDeathScreen()
    {
        if (_instance != null && _instance != this)
        {
            _instance.ShowDeathScreen();
            return;
        }

        gameObject.SetActive(true); 
        if (deathCanvas != null) deathCanvas.gameObject.SetActive(true);
        StartCoroutine(StartGameWithDelay(2f));
    }

    private IEnumerator StartGameWithDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (deathCanvas != null) deathCanvas.SetActive(false);
        StartGame();
    }
}
