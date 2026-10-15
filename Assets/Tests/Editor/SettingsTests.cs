using NUnit.Framework;
using UnityEngine;

/// <summary>
/// settings.json (GDD > Menus > Settings Menu): defaults, clamping, the GDD's resolution list,
/// and the slider-to-decibel conversion the mixer gets.
/// </summary>
public class SettingsTests
{
    private TempSaveFolder _folder;
    private SettingsService _settings;

    [SetUp]
    public void SetUp()
    {
        _folder = new TempSaveFolder();
        _settings = new SettingsService(_folder.Store);
        _settings.Load();
    }

    [TearDown]
    public void TearDown() => _folder.Dispose();

    private SettingsData Reload()
    {
        var reloaded = new SettingsService(_folder.Store);
        reloaded.Load();
        return reloaded.Data;
    }

    [Test]
    public void NoFile_GivesDefaults()
    {
        Assert.AreEqual(1f, _settings.Data.masterVolume);
        Assert.AreEqual(0, _settings.Data.resolutionWidth, "0x0 keeps the native resolution.");
        Assert.AreEqual(FullScreenMode.FullScreenWindow, _settings.Data.FullscreenMode);
        Assert.AreEqual(-1, _settings.Data.lastSlot);
        Assert.IsFalse(_settings.Save(), "Nothing changed, nothing to write.");
        Assert.IsFalse(_folder.Exists(SettingsService.FileName));
    }

    [Test]
    public void Changes_ApplyLive_AndPersistOnSave()
    {
        int audioEvents = 0;
        int displayEvents = 0;
        _settings.AudioChanged += _ => audioEvents++;
        _settings.DisplayChanged += _ => displayEvents++;

        _settings.SetMasterVolume(0.5f);
        _settings.SetMusicVolume(0.25f);
        _settings.SetSfxVolume(0f);
        _settings.SetResolution(1920, 1080);
        _settings.SetFullscreenMode(FullScreenMode.Windowed);
        _settings.SetLastSlot(2);

        Assert.AreEqual(3, audioEvents);
        Assert.AreEqual(2, displayEvents);
        Assert.IsTrue(_settings.Save());

        SettingsData disk = Reload();
        Assert.AreEqual(0.5f, disk.masterVolume);
        Assert.AreEqual(0.25f, disk.musicVolume);
        Assert.AreEqual(0f, disk.sfxVolume);
        Assert.AreEqual(1920, disk.resolutionWidth);
        Assert.AreEqual(1080, disk.resolutionHeight);
        Assert.AreEqual(FullScreenMode.Windowed, disk.FullscreenMode);
        Assert.AreEqual(2, disk.lastSlot);
    }

    [Test]
    public void Volumes_AreClampedToTheSliderRange()
    {
        _settings.SetMasterVolume(3f);
        _settings.SetMusicVolume(-1f);
        _settings.SetSfxVolume(float.NaN);

        Assert.AreEqual(1f, _settings.Data.masterVolume);
        Assert.AreEqual(0f, _settings.Data.musicVolume);
        Assert.AreEqual(1f, _settings.Data.sfxVolume);
    }

    [Test]
    public void ModesOutsideTheMenu_AreIgnored()
    {
        _settings.SetFullscreenMode(FullScreenMode.MaximizedWindow);

        Assert.AreEqual(FullScreenMode.FullScreenWindow, _settings.Data.FullscreenMode);
    }

    [Test]
    public void DamagedFile_GivesDefaults()
    {
        _folder.Write(SettingsService.FileName, "{ nope");

        Assert.AreEqual(1f, Reload().masterVolume);
    }

    [Test]
    public void HandEditedFile_IsRepaired()
    {
        _folder.Write(SettingsService.FileName,
            "{\"version\":1,\"masterVolume\":7,\"resolutionWidth\":-4,\"fullscreenMode\":99,\"lastSlot\":12}");

        SettingsData data = Reload();

        Assert.AreEqual(1f, data.masterVolume);
        Assert.AreEqual(0, data.resolutionWidth);
        Assert.AreEqual(FullScreenMode.FullScreenWindow, data.FullscreenMode);
        Assert.AreEqual(-1, data.lastSlot);
    }

    [Test]
    public void ResolutionList_MatchesTheSettingsMenuSpec()
    {
        Assert.AreEqual(8, DisplaySettings.Resolutions.Length);
        Assert.AreEqual(1, DisplaySettings.IndexOfResolution(1920, 1080));
        Assert.AreEqual(-1, DisplaySettings.IndexOfResolution(800, 600));
        Assert.AreEqual("1920 x 1080 (16:9)", DisplaySettings.ResolutionLabel(1));
        Assert.AreEqual("1920 x 1200 (16:10)", DisplaySettings.ResolutionLabel(4));
        Assert.AreEqual(3, DisplaySettings.ClosestResolution(1280, 719));
        CollectionAssert.AreEqual(
            new[] { FullScreenMode.ExclusiveFullScreen, FullScreenMode.FullScreenWindow, FullScreenMode.Windowed },
            DisplaySettings.Modes);
    }

    [Test]
    public void SliderToDecibels_IsLogarithmic()
    {
        Assert.AreEqual(0f, AudioVolume.ToDecibels(1f), 0.001f);
        Assert.AreEqual(-6.02f, AudioVolume.ToDecibels(0.5f), 0.01f);
        Assert.AreEqual(AudioVolume.MinDecibels, AudioVolume.ToDecibels(0f));
        Assert.AreEqual(AudioVolume.MinDecibels, AudioVolume.ToDecibels(-1f));
    }
}
