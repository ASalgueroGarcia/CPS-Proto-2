using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The menu logic driven through fake views - the same seam a diegetic (in-world) view will
/// plug into. If these pass with fakes, any view implementing the interfaces gets the same flow.
/// </summary>
public class MenuPresenterTests
{
    private TempSaveFolder _folder;
    private SaveService _save;
    private SettingsService _settings;
    private FakeMainMenuView _menuView;
    private FakeSlotView _slotView;
    private FakeSettingsView _settingsView;
    private FakeConfirm _confirm;
    private FakeFlow _flow;
    private SlotSelectPresenter _slots;
    private SettingsPresenter _settingsPresenter;
    private MainMenuPresenter _main;

    [SetUp]
    public void SetUp()
    {
        _folder = new TempSaveFolder();
        _save = new SaveService(_folder.Store);
        _settings = new SettingsService(_folder.Store);
        _settings.Load();

        _menuView = new FakeMainMenuView();
        _slotView = new FakeSlotView();
        _settingsView = new FakeSettingsView();
        _confirm = new FakeConfirm();
        _flow = new FakeFlow();

        _slots = new SlotSelectPresenter(_slotView, _confirm, _save, _flow);
        _settingsPresenter = new SettingsPresenter(_settingsView, _settings);
        _main = new MainMenuPresenter(_menuView, _slots, _settingsPresenter, _flow);
        _menuView.Show();
    }

    [TearDown]
    public void TearDown()
    {
        _main.Dispose();
        _slots.Dispose();
        _settingsPresenter.Dispose();
        _folder.Dispose();
    }

    [Test]
    public void Resume_OpensTheSlots_AndBackReturnsToTheMenu()
    {
        _menuView.ResumeRequested?.Invoke();

        Assert.IsFalse(_menuView.Visible);
        Assert.IsTrue(_slotView.Visible);
        Assert.AreEqual(SaveService.SlotCount, _slotView.Slots.Count);
        Assert.IsFalse(_slotView.Slots[0].Exists);

        _slotView.BackRequested?.Invoke();

        Assert.IsFalse(_slotView.Visible);
        Assert.IsTrue(_menuView.Visible);
    }

    [Test]
    public void ChoosingASlot_StartsTheGameFromIt()
    {
        _menuView.ResumeRequested?.Invoke();

        _slotView.SlotChosen?.Invoke(2);

        Assert.AreEqual(2, _save.ActiveSlot);
        Assert.IsTrue(_save.Data.firstLaunchDone);
        Assert.AreEqual(1, _flow.HubEntered);
        Assert.IsFalse(_slotView.Visible);
    }

    [Test]
    public void TheActiveSlot_IsPreselected()
    {
        _save.SelectSlot(1);

        _menuView.ResumeRequested?.Invoke();

        Assert.AreEqual(1, _slotView.Preselected);
    }

    [Test]
    public void Delete_AsksFirst_AndOnlyDeletesOnYes()
    {
        _save.SelectSlot(0);
        _menuView.ResumeRequested?.Invoke();

        _confirm.Answer = false;
        _slotView.DeleteRequested?.Invoke(0);
        Assert.IsNotNull(_confirm.LastMessage);
        Assert.IsTrue(_save.Peek(0).Exists);

        _confirm.Answer = true;
        _slotView.DeleteRequested?.Invoke(0);
        Assert.IsFalse(_save.Peek(0).Exists);
        Assert.IsFalse(_slotView.Slots[0].Exists, "The cards are refreshed after deleting.");
    }

    [Test]
    public void DeletingAnEmptySlot_DoesNotAsk()
    {
        _menuView.ResumeRequested?.Invoke();

        _slotView.DeleteRequested?.Invoke(1);

        Assert.IsNull(_confirm.LastMessage);
    }

    [Test]
    public void Settings_ShowTheCurrentValues_ApplyChanges_AndSaveOnBack()
    {
        _settings.SetMusicVolume(0.3f);
        _settings.SetResolution(1280, 720);

        _menuView.SettingsRequested?.Invoke();

        Assert.IsFalse(_menuView.Visible);
        Assert.IsTrue(_settingsView.Visible);
        Assert.AreEqual(0.3f, _settingsView.State.MusicVolume);
        Assert.AreEqual(3, _settingsView.State.ResolutionIndex);
        Assert.AreEqual(DisplaySettings.Resolutions.Length, _settingsView.State.ResolutionOptions.Count);
        Assert.AreEqual(1, _settingsView.State.DisplayModeIndex, "Borderless by default.");

        _settingsView.MasterVolumeChanged?.Invoke(0.4f);
        _settingsView.ResolutionChanged?.Invoke(1);
        _settingsView.DisplayModeChanged?.Invoke(2);
        Assert.AreEqual(0.4f, _settings.Data.masterVolume, "Applied live, before Back.");

        _settingsView.BackRequested?.Invoke();

        Assert.IsTrue(_menuView.Visible);
        Assert.IsFalse(_settingsView.Visible);
        var reloaded = new SettingsService(_folder.Store);
        reloaded.Load();
        Assert.AreEqual(0.4f, reloaded.Data.masterVolume);
        Assert.AreEqual(1920, reloaded.Data.resolutionWidth);
        Assert.AreEqual(FullScreenMode.Windowed, reloaded.Data.FullscreenMode);
    }

    [Test]
    public void Exit_QuitsTheGame()
    {
        _menuView.ExitRequested?.Invoke();

        Assert.AreEqual(1, _flow.Quits);
    }

    [Test]
    public void AfterDispose_TheViewsNoLongerDriveAnything()
    {
        _main.Dispose();
        _slots.Dispose();

        _menuView.ResumeRequested?.Invoke();
        _slotView.SlotChosen?.Invoke(0);

        Assert.IsFalse(_slotView.Visible);
        Assert.IsFalse(_save.HasActiveSlot);
        Assert.AreEqual(0, _flow.HubEntered);
    }

    private class FakeMainMenuView : IMainMenuView
    {
        public Action ResumeRequested;
        public Action SettingsRequested;
        public Action ExitRequested;

        public bool Visible { get; private set; }

        event Action IMainMenuView.ResumeRequested { add => ResumeRequested += value; remove => ResumeRequested -= value; }
        event Action IMainMenuView.SettingsRequested { add => SettingsRequested += value; remove => SettingsRequested -= value; }
        event Action IMainMenuView.ExitRequested { add => ExitRequested += value; remove => ExitRequested -= value; }

        public void Show() => Visible = true;
        public void Hide() => Visible = false;
    }

    private class FakeSlotView : ISlotSelectView
    {
        public Action<int> SlotChosen;
        public Action<int> DeleteRequested;
        public Action BackRequested;

        public bool Visible { get; private set; }
        public IReadOnlyList<SlotSummary> Slots { get; private set; }
        public int Preselected { get; private set; } = -1;

        event Action<int> ISlotSelectView.SlotChosen { add => SlotChosen += value; remove => SlotChosen -= value; }
        event Action<int> ISlotSelectView.DeleteRequested { add => DeleteRequested += value; remove => DeleteRequested -= value; }
        event Action ISlotSelectView.BackRequested { add => BackRequested += value; remove => BackRequested -= value; }

        public void Show(IReadOnlyList<SlotSummary> slots, int preselectedSlot)
        {
            Slots = slots;
            Preselected = preselectedSlot;
            Visible = true;
        }

        public void Hide() => Visible = false;
    }

    private class FakeSettingsView : ISettingsView
    {
        public Action<float> MasterVolumeChanged;
        public Action<float> MusicVolumeChanged;
        public Action<float> SfxVolumeChanged;
        public Action<int> ResolutionChanged;
        public Action<int> DisplayModeChanged;
        public Action BackRequested;

        public bool Visible { get; private set; }
        public SettingsViewState State { get; private set; }

        event Action<float> ISettingsView.MasterVolumeChanged { add => MasterVolumeChanged += value; remove => MasterVolumeChanged -= value; }
        event Action<float> ISettingsView.MusicVolumeChanged { add => MusicVolumeChanged += value; remove => MusicVolumeChanged -= value; }
        event Action<float> ISettingsView.SfxVolumeChanged { add => SfxVolumeChanged += value; remove => SfxVolumeChanged -= value; }
        event Action<int> ISettingsView.ResolutionChanged { add => ResolutionChanged += value; remove => ResolutionChanged -= value; }
        event Action<int> ISettingsView.DisplayModeChanged { add => DisplayModeChanged += value; remove => DisplayModeChanged -= value; }
        event Action ISettingsView.BackRequested { add => BackRequested += value; remove => BackRequested -= value; }

        public void Show(SettingsViewState state)
        {
            State = state;
            Visible = true;
        }

        public void Hide() => Visible = false;
    }

    private class FakeConfirm : IConfirmDialog
    {
        public bool Answer { get; set; }
        public string LastMessage { get; private set; }

        public void Ask(string message, Action<bool> onAnswer)
        {
            LastMessage = message;
            onAnswer(Answer);
        }
    }

    private class FakeFlow : IGameFlow
    {
        public int HubEntered { get; private set; }
        public int MainMenus { get; private set; }
        public int Quits { get; private set; }

        public void EnterHub() => HubEntered++;
        public void EnterMainMenu() => MainMenus++;
        public void Quit() => Quits++;
    }
}
