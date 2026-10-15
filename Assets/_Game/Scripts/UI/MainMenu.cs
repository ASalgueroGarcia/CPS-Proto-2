using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// First screen of the game (GDD > Menus: Art and main functions > Main Menu): Resume,
/// Settings, Exit. The Canvas version of IMainMenuView - the buttons only report the click and
/// MainMenuPresenter decides what happens, so an in-world menu can replace this one later.
/// </summary>
public class MainMenu : MonoBehaviour, IMainMenuView
{
    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;

    [Header("Default Selection (keyboard / gamepad)")]
    [SerializeField] private Button playButton;

    public event Action ResumeRequested;
    public event Action SettingsRequested;
    public event Action ExitRequested;

    // Button OnClick targets. The names predate the save system; the prefab is wired to them.
    public void Play() => ResumeRequested?.Invoke();

    public void OpenSettings() => SettingsRequested?.Invoke();

    public void Exit() => ExitRequested?.Invoke();

    public void Show(Action onShown = null)
    {
        menuPanel.SetActive(true);
        Select(playButton);
        onShown?.Invoke();
    }

    public void Hide(Action onHidden = null)
    {
        menuPanel.SetActive(false);
        onHidden?.Invoke();
    }

    private static void Select(Selectable selectable)
    {
        if (selectable != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
