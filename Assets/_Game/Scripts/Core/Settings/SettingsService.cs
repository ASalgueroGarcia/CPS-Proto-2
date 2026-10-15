using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads, changes and writes settings.json. Changes apply live through the events (sound and
/// screen listen to them); the file is written when the player leaves the menu, not on every
/// slider tick.
/// </summary>
public class SettingsService
{
    public const string FileName = "settings.json";

    private readonly JsonFileStore _store;
    private bool _dirty;
    private bool _readOnly;

    /// <summary>A volume changed - the sound side re-applies the mixer.</summary>
    public event Action<SettingsData> AudioChanged;

    /// <summary>Resolution or window mode changed - the screen gets resized.</summary>
    public event Action<SettingsData> DisplayChanged;

    public SettingsData Data { get; private set; } = new SettingsData();

    public SettingsService(JsonFileStore store)
    {
        _store = store;
    }

    public void Load()
    {
        _dirty = false;
        _readOnly = false;
        Data = new SettingsData();

        if (!_store.Exists(FileName)) return;

        string json;
        try
        {
            json = _store.Read(FileName);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            // Keep defaults for this session, but don't replace a file we couldn't read.
            Debug.LogWarning($"[SettingsService] Could not read {FileName}: {e.Message}");
            _readOnly = true;
            return;
        }

        Data = SettingsData.Parse(json, out SaveParseResult result);
        if (result == SaveParseResult.Corrupt)
        {
            Debug.LogWarning($"[SettingsService] {FileName} was unreadable - using default settings.");
        }
    }

    public void SetMasterVolume(float value) => SetVolume(ref Data.masterVolume, value);
    public void SetMusicVolume(float value) => SetVolume(ref Data.musicVolume, value);
    public void SetSfxVolume(float value) => SetVolume(ref Data.sfxVolume, value);

    public void SetResolution(int width, int height)
    {
        if (width == Data.resolutionWidth && height == Data.resolutionHeight) return;

        Data.resolutionWidth = width;
        Data.resolutionHeight = height;
        Data.Normalize();
        _dirty = true;
        DisplayChanged?.Invoke(Data);
    }

    public void SetFullscreenMode(FullScreenMode mode)
    {
        if (DisplaySettings.IndexOfMode(mode) < 0 || (int)mode == Data.fullscreenMode) return;

        Data.fullscreenMode = (int)mode;
        _dirty = true;
        DisplayChanged?.Invoke(Data);
    }

    public void SetLastSlot(int slot)
    {
        if (!SaveService.IsValidSlot(slot) || slot == Data.lastSlot) return;

        Data.lastSlot = slot;
        _dirty = true;
    }

    /// <summary>Writes the file if anything changed since the last write.</summary>
    public bool Save()
    {
        if (!_dirty || _readOnly) return false;

        try
        {
            _store.WriteAtomic(FileName, SaveSerializer.ToJson(Data));
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[SettingsService] Saving {FileName} failed: {e.Message}");
            return false;
        }

        _dirty = false;
        return true;
    }

    private void SetVolume(ref float field, float value)
    {
        float volume = SettingsData.ToVolume(value);
        if (Mathf.Approximately(field, volume)) return;

        field = volume;
        _dirty = true;
        AudioChanged?.Invoke(Data);
    }
}
