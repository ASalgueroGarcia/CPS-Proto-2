
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
    
/*    [Header("GAME UI")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI damageText;
*/

    private PlayerStatsManager _playerStats;
    private Health _playerHealth;
    private bool _isPaused = false;

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
        _playerStats = FindFirstObjectByType<PlayerStatsManager>();
        _playerHealth = FindFirstObjectByType<Health>();
        
        if (pausePanel != null){
            pausePanel.SetActive(false);
        }

        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePressed;
        }

        //SceneController.Instance._onMapLoaded += Resume;
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
        }
        //SceneController.Instance._onMapLoaded -= Resume;
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
/*    private void Update()
    {
        // ACTUALIZAR UI
        if (playerHealth != null && healthText != null && !isPaused)
        {
            healthText.text = $"Health: {playerHealth.currentHealth}/{playerHealth.maxHealth}";
        }
    }
*/
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