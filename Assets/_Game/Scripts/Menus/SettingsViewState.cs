using System.Collections.Generic;

/// <summary>Everything the settings view needs to draw itself, in one snapshot.</summary>
public class SettingsViewState
{
    public float MasterVolume { get; set; }
    public float MusicVolume { get; set; }
    public float SfxVolume { get; set; }

    public IReadOnlyList<string> ResolutionOptions { get; set; }
    public int ResolutionIndex { get; set; }

    public IReadOnlyList<string> DisplayModeOptions { get; set; }
    public int DisplayModeIndex { get; set; }
}
