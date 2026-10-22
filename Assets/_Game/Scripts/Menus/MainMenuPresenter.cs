using System;
using UnityEngine;

/// <summary>
/// Title screen logic (GDD > Menus > Main Menu): Resume opens the save slots ("start the game
/// from the last save"), Settings opens the settings, Exit closes the game. Knows only the
/// view interfaces, so the screen can become an in-world object without changes here.
/// Every move waits for the view's hide / show to finish and ignores input in between.
/// </summary>
public class MainMenuPresenter : IDisposable
{
    private readonly IMainMenuView _view;
    private readonly SlotSelectPresenter _slots;
    private readonly SettingsPresenter _settings;
    private readonly IGameFlow _flow;
    private bool _busy = true;

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

    /// <summary>Shows the title screen; it takes input once the view says it's up.</summary>
    public void Open()
    {
        _busy = true;
        _view.Show(() => _busy = false);
    }

    private void OnResume()
    {
        if (_busy) return;
        _busy = true;
        _view.Hide(() => _slots.Open(Open));
    }

    private void OnSettings()
    {
        if (_busy) return;
        _busy = true;
        _view.Hide(() => _settings.Open(Open));
    }

    private void OnExit()
    {
        if (_busy) return;
        if (_flow == null)
        {
            Debug.LogError("[MainMenuPresenter] No IGameFlow registered - can't quit.");
            return;
        }

        _busy = true;
        _flow.Quit();
    }
}
