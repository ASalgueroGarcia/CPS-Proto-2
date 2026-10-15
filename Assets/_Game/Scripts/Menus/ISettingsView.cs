using System;

/// <summary>
/// The Settings Menu as the menu logic sees it (GDD > Menus > Settings Menu: three volume
/// sliders, resolution and window mode dropdowns, Back). Values arrive as linear 0-1 volumes
/// and option indexes; the view never touches SettingsData.
/// </summary>
public interface ISettingsView
{
    event Action<float> MasterVolumeChanged;
    event Action<float> MusicVolumeChanged;
    event Action<float> SfxVolumeChanged;
    event Action<int> ResolutionChanged;
    event Action<int> DisplayModeChanged;
    event Action BackRequested;

    void Show(SettingsViewState state);
    void Hide();
}
