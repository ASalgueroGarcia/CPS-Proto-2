using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// First screen of the game (GDD > Menus: Art and main functions > Main Menu).
/// Play starts a new run, Settings opens the settings panel, Exit closes the game.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Default Selection (keyboard / gamepad)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsBackButton;

    private void Start()
    {
        CloseSettings();
    }

    /// <summary>
    /// Starts a new run. There is no save system yet, so this is always a fresh run.
    /// </summary>
    public void Play()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[MainMenu] UIManager instance is null! Cannot start the game.", this);
            return;
        }

        UIManager.Instance.StartGame();
    }

    /// <summary>
    /// Placeholder until the Settings Menu (volume sliders, controls) is built.
    /// </summary>
    public void OpenSettings()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        Select(settingsBackButton);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        menuPanel.SetActive(true);
        Select(playButton);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Select(Button button)
    {
        if (button != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
