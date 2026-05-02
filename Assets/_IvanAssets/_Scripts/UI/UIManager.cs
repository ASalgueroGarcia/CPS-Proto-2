using System.Collections;
using System.Collections.Generic;
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

    [Header("PLAYER UI")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI specialCDText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI inventoryText;
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private Image healthBar;
    [SerializeField] private Image cooldown;
    
    [Header("CANVAS REFERENCES")]
    [SerializeField] private GameObject playerUICanvas;
    [SerializeField] private GameObject deathCanvas;
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private GameObject eolCanvas;
    
    private bool _isPaused = false;
    private Health _playerHealth;
    private PlayerStatsManager _playerStats;
    private PlayerFSM _playerFsm;
    private static UIManager _instance;

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
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        RefreshHUDVisibility();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Try to find the canvas if it's lost
        if (playerUICanvas == null)
        {
            playerUICanvas = FindChildByName(transform, "PlayerUICanvas");
        }

        // Force resume if entering a gameplay room to prevent being stuck in pause state
        if (scene.name == "SetScene")
        {
            Resume();
        }

        // Hide Player UI in Main Menu and Map Scene
        RefreshHUDVisibility();

        // Refresh references when a new scene is loaded
        _playerFsm = FindFirstObjectByType<PlayerFSM>();
        _playerStats = PlayerStatsManager.Instance;
        
        // Unsubscribe from old health component if any
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.RemoveListener(OnPlayerHealthChanged);
        }

        // FIND PLAYER HEALTH SPECIFICALLY via PlayerFSM to avoid finding enemies
        if (_playerFsm != null)
        {
            _playerHealth = _playerFsm.GetComponent<Health>();
            
            // Re-bind stats manager to new player when a new level is loaded additively
            if (PlayerStatsManager.Instance != null)
            {
                PlayerStatsManager.Instance.BindToPlayer();
            }
        }
        else
        {
            // Fallback: search for object tagged Player
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) _playerHealth = playerObj.GetComponent<Health>();
        }
        
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.AddListener(OnPlayerHealthChanged);
            OnPlayerHealthChanged(_playerHealth.currentHealth, _playerHealth.maxHealth);
        }
    }

    private void OnPlayerHealthChanged(float current, float max)
    {
        if (playerHealthSlider != null)
        {
            
            //playerHealthSlider.maxValue = max;
            //playerHealthSlider.value = current;
            healthBar.fillAmount = current / max;
            //healthBar.rectTransform.rect.width = max * 300;

        }

        if (healthText != null)
        {
            healthText.text = $"{(int)current} / {(int)max}";
        }
    }

    private void RefreshHUDVisibility()
    {
        if (playerUICanvas == null) return;

        string currentBaseScene = SceneManager.GetActiveScene().name;
        bool isMenu = currentBaseScene == "MainMenu" || currentBaseScene == "UI_Basic";
        
        // HUD should be visible in ANY scene that isn't a menu, provided a player exists
        bool hasPlayer = _playerFsm != null || GameObject.FindWithTag("Player") != null;
        bool shouldBeVisible = !isMenu && hasPlayer;
        
        if (playerUICanvas.activeSelf != shouldBeVisible)
        {
            playerUICanvas.SetActive(shouldBeVisible);
        }

        // Clear text if hiding to prevent ghosting
        if (!shouldBeVisible && healthText != null) healthText.text = "";
    }

    private void Start()
    {
        _playerFsm = FindFirstObjectByType<PlayerFSM>();
        _playerStats = FindFirstObjectByType<PlayerStatsManager>();

        if (playerUICanvas == null) playerUICanvas = FindChildByName(transform, "PlayerUICanvas");

        if (_playerHealth != null) _playerHealth.OnHealthChanged.RemoveListener(OnPlayerHealthChanged);
        
        // FIND PLAYER HEALTH SPECIFICALLY via PlayerFSM
        if (_playerFsm != null)
        {
            _playerHealth = _playerFsm.GetComponent<Health>();
        }
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) _playerHealth = playerObj.GetComponent<Health>();
        }
        
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.AddListener(OnPlayerHealthChanged);
            OnPlayerHealthChanged(_playerHealth.currentHealth, _playerHealth.maxHealth);
        }

        // Try to find missing references in children
        if (playerHealthSlider == null) playerHealthSlider = GetComponentInChildren<Slider>(true);
        if (healthText == null || speedText == null || damageText == null || specialCDText == null || comboText == null || coinsText == null || inventoryText == null)
        {
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in allTexts)
            {
                if (healthText == null && txt.name.Contains("Health", System.StringComparison.OrdinalIgnoreCase)) healthText = txt;
                if (speedText == null && txt.name.Contains("Speed", System.StringComparison.OrdinalIgnoreCase)) speedText = txt;
                if (damageText == null && txt.name.Contains("Damage", System.StringComparison.OrdinalIgnoreCase)) damageText = txt;
                if (specialCDText == null && txt.name.Contains("Special", System.StringComparison.OrdinalIgnoreCase)) specialCDText = txt;
                if (comboText == null && txt.name.Contains("Combo", System.StringComparison.OrdinalIgnoreCase)) comboText = txt;
                if (coinsText == null && txt.name.Contains("Coin", System.StringComparison.OrdinalIgnoreCase)) coinsText = txt;
                if (inventoryText == null && (txt.name.Contains("Inventory", System.StringComparison.OrdinalIgnoreCase) || txt.name.Contains("Item", System.StringComparison.OrdinalIgnoreCase))) inventoryText = txt;
            }
        }

        // Final forced update after finding all elements
        if (_playerHealth != null)
        {
            OnPlayerHealthChanged(_playerHealth.currentHealth, _playerHealth.maxHealth);
        }

        if (pausePanel == null) pausePanel = FindChildByName(transform, "PausePanel");
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (eolCanvas == null) eolCanvas = FindChildByName(transform, "EoLCanvas");
        if (eolCanvas != null)
        {
            eolCanvas.SetActive(false);
        }

        if (deathCanvas == null) deathCanvas = FindChildByName(transform, "DeathCanvas");
        if (deathCanvas != null)
        {
            deathCanvas.SetActive(false);
        }

        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePressed;
        }

        if (_playerHealth != null)
        {
            OnPlayerHealthChanged(_playerHealth.currentHealth, _playerHealth.maxHealth);
        }

        // Initial visibility check
        RefreshHUDVisibility();
    }

    private GameObject FindChildByName(Transform root, string childName)
    {
        Transform[] trs = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in trs)
        {
            if (t.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return t.gameObject;
            }
        }
        return null;
    }

    // Removed EnsureUIElementsExist method to allow designers to modify UI in Canvases

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

        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.RemoveListener(OnPlayerHealthChanged);
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
        // Safety check: Ensure HUD stays hidden in non-gameplay scenes
        RefreshHUDVisibility();

        if (_isPaused) return;

        if (playerUICanvas != null && playerUICanvas.activeSelf)
        {
            UpdatePlayerUI();
        }
    }

    private void UpdatePlayerUI()
    {
        if (_playerHealth == null)
        {
            _playerFsm = FindFirstObjectByType<PlayerFSM>();
            if (_playerFsm != null)
            {
                _playerHealth = _playerFsm.GetComponent<Health>();
                if (_playerHealth != null)
                {
                    _playerHealth.OnHealthChanged.AddListener(OnPlayerHealthChanged);
                    OnPlayerHealthChanged(_playerHealth.currentHealth, _playerHealth.maxHealth);
                }
            }
            
            _playerStats = FindFirstObjectByType<PlayerStatsManager>();
        }

        if (_playerFsm != null)
        {
            if (speedText != null)
            {
                speedText.text = $"Speed: {_playerFsm.speed:F1}";
            }

            if (damageText != null)
            {
                damageText.text = $"DMG: {_playerFsm.weaponBaseDamage:F1}";
            }

            if (specialCDText != null)
            {
                if (_playerFsm.specialTimer > 0)
                {
                    cooldown.fillAmount += _playerFsm.specialTimer;
                    specialCDText.text = $"{_playerFsm.specialTimer:F1}s";
                }
                    

                else
                {
                    specialCDText.text = "READY";
                    cooldown.fillAmount = float.MaxValue;
                }
                    
            }

            if (comboText != null)
            {
                comboText.text = $"{_playerFsm.comboStep}";
            }
        }

        if (_playerStats != null)
        {
            if (coinsText != null)
            {
                coinsText.text = $"{_playerStats.CurrentCoins} €";
            }

            if (inventoryText != null)
            {
                string itemNames = "Items: ";
                if (_playerStats.InventoryItems.Count > 0)
                {
                    List<string> names = new List<string>();
                    foreach (var item in _playerStats.InventoryItems)
                    {
                        names.Add(item.powerUpName);
                    }
                    itemNames += string.Join(", ", names);
                }
                else
                {
                    itemNames += "None";
                }
                inventoryText.text = itemNames;
            }
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        PlayerFSM.IsPaused = false;

        if (mainMenuCanvas != null) mainMenuCanvas.gameObject.SetActive(false);
        
        // Forcibly hide HUD before transition
        if (playerUICanvas != null) playerUICanvas.SetActive(false);

        // Reset stats if possible
        if (_playerStats == null) _playerStats = FindFirstObjectByType<PlayerStatsManager>();
        if (_playerStats != null) _playerStats.ResetAllThePlayerStats();
        
        RefreshHUDVisibility();
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
        PlayerFSM.IsPaused = false;
    }
    
    public void ReturnToMap()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        PlayerFSM.IsPaused = false;
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
