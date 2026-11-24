using System;
using UnityEngine;

/// <summary>
/// Player options, shared by all save slots and kept in their own file
/// (GDD > Save System: "Settings changed - written to a separate settings file").
/// Volumes are linear 0-1; a 0x0 resolution means "keep the monitor's native one".
/// </summary>
[Serializable]
public class SettingsData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;

    public int resolutionWidth;
    public int resolutionHeight;

    /// <summary>A UnityEngine.FullScreenMode stored as int (JsonUtility writes enums as numbers anyway).</summary>
    public int fullscreenMode = (int)FullScreenMode.FullScreenWindow;

    /// <summary>The slot the player played last, preselected on the slot screen (-1 = none yet).</summary>
    public int lastSlot = -1;

    public FullScreenMode FullscreenMode => (FullScreenMode)fullscreenMode;

    public static SettingsData Parse(string json, out SaveParseResult result)
    {
        var data = new SettingsData();
        result = SaveSerializer.TryOverwrite(json, data, CurrentVersion, out _);
        if (result == SaveParseResult.Corrupt) return new SettingsData();

        data.version = CurrentVersion;
        data.Normalize();
        return data;
    }

    public void Normalize()
    {
        masterVolume = ToVolume(masterVolume);
        musicVolume = ToVolume(musicVolume);
        sfxVolume = ToVolume(sfxVolume);

        // Only the menu's sizes or native: a hand-edited 100000 x 5 would be applied at every boot.
        if (DisplaySettings.IndexOfResolution(resolutionWidth, resolutionHeight) < 0)
        {
            resolutionWidth = 0;
            resolutionHeight = 0;
        }

        if (DisplaySettings.IndexOfMode(FullscreenMode) < 0)
        {
            fullscreenMode = (int)FullScreenMode.FullScreenWindow;
        }

        if (!SaveService.IsValidSlot(lastSlot))
        {
            lastSlot = -1;
        }
    }

    public static float ToVolume(float value) => float.IsNaN(value) ? 1f : Mathf.Clamp01(value);
}
