using System;
using UnityEngine;

/// <summary>
/// Settings Menu logic (GDD > Menus > Settings Menu). Changes apply live; Back writes
/// settings.json and returns to whoever opened it - the Main Menu or, later, the Pause Menu.
/// </summary>
public class SettingsPresenter : IDisposable
{
    private static readonly string[] ResolutionOptions = BuildResolutionOptions();
    private static readonly string[] DisplayModeOptions = BuildDisplayModeOptions();

    private readonly ISettingsView _view;
    private readonly SettingsService _settings;
    private Action _onBack;
    private bool _busy = true;

    public SettingsPresenter(ISettingsView view, SettingsService settings)
    {
        _view = view;
        _settings = settings;

        _view.MasterVolumeChanged += _settings.SetMasterVolume;
        _view.MusicVolumeChanged += _settings.SetMusicVolume;
        _view.SfxVolumeChanged += _settings.SetSfxVolume;
        _view.ResolutionChanged += OnResolutionChanged;
        _view.DisplayModeChanged += OnDisplayModeChanged;
        _view.BackRequested += OnBack;
    }

    public void Dispose()
    {
        _view.MasterVolumeChanged -= _settings.SetMasterVolume;
        _view.MusicVolumeChanged -= _settings.SetMusicVolume;
        _view.SfxVolumeChanged -= _settings.SetSfxVolume;
        _view.ResolutionChanged -= OnResolutionChanged;
        _view.DisplayModeChanged -= OnDisplayModeChanged;
        _view.BackRequested -= OnBack;
    }

    /// <summary>Shows the current settings; <paramref name="onBack"/> runs after Back saved them.</summary>
    public void Open(Action onBack)
    {
        _onBack = onBack;
        _busy = true;
        _view.SetState(BuildState());
        _view.Show(() => _busy = false);
    }

    private SettingsViewState BuildState()
    {
        SettingsData data = _settings.Data;
        return new SettingsViewState
        {
            MasterVolume = data.masterVolume,
            MusicVolume = data.musicVolume,
            SfxVolume = data.sfxVolume,
            ResolutionOptions = ResolutionOptions,
            ResolutionIndex = CurrentResolutionIndex(data),
            DisplayModeOptions = DisplayModeOptions,
            DisplayModeIndex = Mathf.Max(0, DisplaySettings.IndexOfMode(data.FullscreenMode)),
        };
    }

    // "Native" (0x0) isn't one of the GDD's options - show the closest one to the screen.
    private static int CurrentResolutionIndex(SettingsData data)
    {
        int index = DisplaySettings.IndexOfResolution(data.resolutionWidth, data.resolutionHeight);
        return index >= 0 ? index : DisplaySettings.ClosestResolution(Screen.width, Screen.height);
    }

    private void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= DisplaySettings.Resolutions.Length) return;

        Vector2Int size = DisplaySettings.Resolutions[index];
        _settings.SetResolution(size.x, size.y);
    }

    private void OnDisplayModeChanged(int index)
    {
        if (index < 0 || index >= DisplaySettings.Modes.Length) return;

        // A window at "native" size would be as big as the desktop: take the resolution the menu shows.
        FullScreenMode mode = DisplaySettings.Modes[index];
        if (mode == FullScreenMode.Windowed && _settings.Data.resolutionWidth == 0)
        {
            OnResolutionChanged(CurrentResolutionIndex(_settings.Data));
        }

        _settings.SetFullscreenMode(mode);
    }

    private void OnBack()
    {
        if (_busy) return;
        _busy = true;
        _settings.Save();
        _view.Hide(() => _onBack?.Invoke());
    }

    private static string[] BuildResolutionOptions()
    {
        var options = new string[DisplaySettings.Resolutions.Length];
        for (int i = 0; i < options.Length; i++)
        {
            options[i] = DisplaySettings.ResolutionLabel(i);
        }

        return options;
    }

    private static string[] BuildDisplayModeOptions()
    {
        var options = new string[DisplaySettings.Modes.Length];
        for (int i = 0; i < options.Length; i++)
        {
            options[i] = DisplaySettings.ModeLabel(i);
        }

        return options;
    }
}
