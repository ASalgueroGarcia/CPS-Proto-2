using System;
using UnityEngine;

/// <summary>
/// Title screen logic (GDD > Menus > Main Menu): Resume opens the save slots ("start the game
/// from the last save"), Settings opens the settings, Exit closes the game. Knows only the
/// view interfaces, so the screen can become an in-world object without changes here.
/// </summary>
public class MainMenuPresenter : IDisposable
{
    private readonly IMainMenuView _view;
    private readonly SlotSelectPresenter _slots;
    private readonly SettingsPresenter _settings;
    private readonly IGameFlow _flow;

    public MainMenuPresenter(IMainMenuView view, SlotSelectPresenter slots, SettingsPresenter settings, IGameFlow flow)
    {
        _view = view;
        _slots = slots;
        _settings = settings;
        _flow = flow;

        _view.ResumeRequested += OnResume;
        _view.SettingsRequested += OnSettings;
        _view.ExitRequested += OnExit;
    }

    public void Dispose()
    {
        _view.ResumeRequested -= OnResume;
        _view.SettingsRequested -= OnSettings;
        _view.ExitRequested -= OnExit;
    }

    private void OnResume()
    {
        _view.Hide();
        _slots.Open(_view.Show);
    }

    private void OnSettings()
    {
        _view.Hide();
        _settings.Open(_view.Show);
    }

    private void OnExit()
    {
        if (_flow == null)
        {
            Debug.LogError("[MainMenuPresenter] No IGameFlow registered - can't quit.");
            return;
        }

        _flow.Quit();
    }
}
