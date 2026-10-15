using System;
using UnityEngine;

/// <summary>
/// Settings Menu logic (GDD > Menus > Settings Menu). Changes apply live; Back writes
/// settings.json and returns to whoever opened it - the Main Menu or, later, the Pause Menu.
/// </summary>
public class SettingsPresenter : IDisposable
{
    // The first resolution entry is "Native", the default; the GDD's sizes follow it.
    private const int NativeIndex = 0;

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
            ResolutionOptions = BuildResolutionOptions(),
            ResolutionIndex = CurrentResolutionIndex(data),
            DisplayModeOptions = DisplayModeOptions,
            DisplayModeIndex = Mathf.Max(0, DisplaySettings.IndexOfMode(data.FullscreenMode)),
        };
    }

    // Native (0x0) isn't in the GDD's list, so it gets its own entry: the menu always shows the real
    // setting, and every other entry is a change (a dropdown ignores picking the entry it shows).
    private static int CurrentResolutionIndex(SettingsData data) =>
        DisplaySettings.IndexOfResolution(data.resolutionWidth, data.resolutionHeight) + 1;

    private void OnResolutionChanged(int index)
    {
        if (index < NativeIndex || index > DisplaySettings.Resolutions.Length) return;

        if (index == NativeIndex)
        {
            // A window at native size would be as big as the desktop: a window keeps its size.
            if (_settings.Data.FullscreenMode == FullScreenMode.Windowed)
            {
                _view.SetState(BuildState());
                return;
            }

            _settings.SetResolution(0, 0);
            return;
        }

        Vector2Int size = DisplaySettings.Resolutions[index - 1];
        _settings.SetResolution(size.x, size.y);
    }

    private void OnDisplayModeChanged(int index)
    {
        if (index < 0 || index >= DisplaySettings.Modes.Length) return;

        // A window at native size would be as big as the desktop: take the listed size closest to
        // the screen, and redraw so the resolution entry shows it.
        FullScreenMode mode = DisplaySettings.Modes[index];
        if (mode == FullScreenMode.Windowed && _settings.Data.resolutionWidth == 0)
        {
            Vector2Int size = DisplaySettings.Resolutions[DisplaySettings.ClosestResolution(Screen.width, Screen.height)];
            _settings.SetResolution(size.x, size.y);
        }

        _settings.SetFullscreenMode(mode);
        _view.SetState(BuildState());
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
        Resolution screen = Screen.currentResolution;
        var options = new string[DisplaySettings.Resolutions.Length + 1];
        options[NativeIndex] = $"Native ({screen.width} x {screen.height})";
        for (int i = 0; i < DisplaySettings.Resolutions.Length; i++)
        {
            options[i + 1] = DisplaySettings.ResolutionLabel(i);
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
