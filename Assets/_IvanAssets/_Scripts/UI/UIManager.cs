using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("PAUSE MENU")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private InputActionReference pauseAction;

    [SerializeField] private Button inGamePauseButton;

/*    [Header("GAME UI")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI damageText;
*/

    private PlayerStatsManager playerStats;
    private Health playerHealth;
    private bool isPaused = false;

    private void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStatsManager>();
        playerHealth = FindFirstObjectByType<Health>();
        
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
        if (isPaused){
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
        isPaused = true;
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
        isPaused = false;
    }
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("UI_Basic");
    }
}