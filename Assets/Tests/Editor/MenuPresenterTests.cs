using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The menu logic driven through fake views - the same seam a diegetic (in-world) view will
/// plug into. The fakes can hold their show / hide "animations" open, like an in-world menu
/// would, to prove the presenters wait for them and ignore input meanwhile.
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
        _main.Open();
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
    public void ALockedSlot_IsReadAgain_InsteadOfPlayedFromNothing()
    {
        new SaveService(_folder.Store).SelectSlot(0);
        _menuView.ResumeRequested?.Invoke();

        using (new FileStream(_folder.FilePath(SaveService.FileNameOf(0)), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            _slotView.SlotChosen?.Invoke(0);

            Assert.IsFalse(_save.HasActiveSlot);
            Assert.AreEqual(0, _flow.HubEntered);
            Assert.IsTrue(_slotView.Visible);
            Assert.IsTrue(_slotView.Slots[0].Locked);
        }

        _slotView.SlotChosen?.Invoke(0);

        Assert.AreEqual(0, _save.ActiveSlot, "Plays once the other program lets go.");
        Assert.AreEqual(1, _flow.HubEntered);
    }

    [Test]
    public void ChoosingASlot_StartsTheGameFromIt()
    {
        _menuView.ResumeRequested?.Invoke();

        _slotView.SlotChosen?.Invoke(2);

        Assert.AreEqual(2, _save.ActiveSlot);
        Assert.IsFalse(_save.Data.firstLaunchDone, "Set by the first run, so a lobby intro can still see it.");
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

        _slotView.DeleteRequested?.Invoke(0);
        Assert.IsNotNull(_confirm.LastMessage);
        _confirm.Answer(false);
        Assert.IsTrue(_save.Peek(0).Exists);

        _slotView.DeleteRequested?.Invoke(0);
        _confirm.Answer(true);
        Assert.IsFalse(_save.Peek(0).Exists);
        Assert.IsFalse(_slotView.Slots[0].Exists, "The cards are redrawn after deleting.");
        Assert.AreEqual(1, _slotView.ShowCount, "Redrawing doesn't replay the show animation.");
    }

    [Test]
    public void DeletingAnEmptySlot_DoesNotAsk()
    {
        _menuView.ResumeRequested?.Invoke();

        _slotView.DeleteRequested?.Invoke(1);

        Assert.IsNull(_confirm.LastMessage);
    }

    [Test]
    public void WhileTheDeleteQuestionIsOpen_TheSlotsTakeNoInput()
    {
        _save.SelectSlot(0);
        _menuView.ResumeRequested?.Invoke();
        _slotView.DeleteRequested?.Invoke(0);

        _slotView.SlotChosen?.Invoke(1);
        _slotView.DeleteRequested?.Invoke(0);
        _slotView.BackRequested?.Invoke();

        Assert.AreEqual(0, _flow.HubEntered, "no game started behind the dialog");
        Assert.AreEqual(1, _confirm.Questions, "no second question replacing the first");
        Assert.IsTrue(_slotView.Visible, "still on the slot screen");

        _confirm.Answer(false);
        _slotView.SlotChosen?.Invoke(1);
        Assert.AreEqual(1, _flow.HubEntered, "input comes back once the question is answered");
    }

    [Test]
    public void TransitionsWaitForTheViews_AndIgnoreInputMeanwhile()
    {
        _menuView.Deferred = true;
        _slotView.Deferred = true;

        _menuView.ResumeRequested?.Invoke();
        _menuView.ResumeRequested?.Invoke();
        _menuView.SettingsRequested?.Invoke();
        Assert.IsFalse(_slotView.Visible, "the slots wait for the title to finish hiding");

        _menuView.Complete();
        Assert.AreEqual(1, _slotView.ShowCount, "the slots start showing once the title is gone");
        Assert.AreEqual(0, _settingsView.ShowCount, "the second press was ignored");

        _slotView.SlotChosen?.Invoke(0);
        Assert.AreEqual(-1, _save.ActiveSlot, "no input until the slots finished showing");

        _slotView.Complete();
        Assert.IsTrue(_slotView.Visible);
        _slotView.SlotChosen?.Invoke(0);
        _slotView.SlotChosen?.Invoke(1);
        _slotView.Complete();

        Assert.AreEqual(0, _save.ActiveSlot, "a double press picks one slot");
        Assert.AreEqual(1, _flow.HubEntered, "and starts one game, after the slots hid");
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
        Assert.AreEqual(4, _settingsView.State.ResolutionIndex, "1280 x 720, after Native and three others.");
        Assert.AreEqual(DisplaySettings.Resolutions.Length + 1, _settingsView.State.ResolutionOptions.Count);
        Assert.AreEqual(1, _settingsView.State.DisplayModeIndex, "Borderless by default.");

        _settingsView.MasterVolumeChanged?.Invoke(0.4f);
        _settingsView.ResolutionChanged?.Invoke(2);
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
    public void Native_IsTheFirstEntry_SoTheMenuNeverShowsAGuess()
    {
        _menuView.SettingsRequested?.Invoke();
        Assert.AreEqual(0, _settings.Data.resolutionWidth, "native until the player picks one");
        Assert.AreEqual(0, _settingsView.State.ResolutionIndex);
        StringAssert.StartsWith("Native (", _settingsView.State.ResolutionOptions[0]);

        _settingsView.ResolutionChanged?.Invoke(1);
        Assert.AreEqual(2560, _settings.Data.resolutionWidth, "every listed size is a real change");

        _settingsView.ResolutionChanged?.Invoke(0);
        Assert.AreEqual(0, _settings.Data.resolutionWidth, "and Native goes back");
    }

    [Test]
    public void Windowed_WithoutAResolution_TakesAListedSize_AndTheMenuShowsIt()
    {
        _menuView.SettingsRequested?.Invoke();

        _settingsView.DisplayModeChanged?.Invoke(2);

        Assert.AreEqual(FullScreenMode.Windowed, _settings.Data.FullscreenMode);
        Assert.AreNotEqual(0, _settings.Data.resolutionWidth, "switching to windowed never opens a desktop-sized window by itself");
        Vector2Int shown = DisplaySettings.Resolutions[_settingsView.State.ResolutionIndex - 1];
        Assert.AreEqual(shown.x, _settings.Data.resolutionWidth);
        Assert.AreEqual(shown.y, _settings.Data.resolutionHeight);

        _settingsView.ResolutionChanged?.Invoke(0);
        Assert.AreEqual(0, _settings.Data.resolutionWidth, "picking Native while windowed is the player's choice: a screen-sized window");
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

    // Show / Hide finish at once, or - with Deferred - only when the test calls Complete(),
    // like an in-world view whose animation is still playing.
    private abstract class FakeView : IMenuView
    {
        private readonly Queue<Action> _pending = new Queue<Action>();

        public bool Deferred { get; set; }
        public bool Visible { get; private set; }
        public int ShowCount { get; private set; }

        public void Show(Action onShown = null)
        {
            ShowCount++;
            Run(() => Visible = true, onShown);
        }

        public void Hide(Action onHidden = null) => Run(() => Visible = false, onHidden);

        public void Complete()
        {
            while (_pending.Count > 0) _pending.Dequeue()();
        }

        private void Run(Action change, Action done)
        {
            if (Deferred)
            {
                _pending.Enqueue(() => { change(); done?.Invoke(); });
                return;
            }

            change();
            done?.Invoke();
        }
    }

    private class FakeMainMenuView : FakeView, IMainMenuView
    {
        public Action ResumeRequested;
        public Action SettingsRequested;
        public Action ExitRequested;

        event Action IMainMenuView.ResumeRequested { add => ResumeRequested += value; remove => ResumeRequested -= value; }
        event Action IMainMenuView.SettingsRequested { add => SettingsRequested += value; remove => SettingsRequested -= value; }
        event Action IMainMenuView.ExitRequested { add => ExitRequested += value; remove => ExitRequested -= value; }
    }

    private class FakeSlotView : FakeView, ISlotSelectView
    {
        public Action<int> SlotChosen;
        public Action<int> DeleteRequested;
        public Action BackRequested;

        public IReadOnlyList<SlotSummary> Slots { get; private set; }
        public int Preselected { get; private set; } = -1;

        event Action<int> ISlotSelectView.SlotChosen { add => SlotChosen += value; remove => SlotChosen -= value; }
        event Action<int> ISlotSelectView.DeleteRequested { add => DeleteRequested += value; remove => DeleteRequested -= value; }
        event Action ISlotSelectView.BackRequested { add => BackRequested += value; remove => BackRequested -= value; }

        public void SetSlots(IReadOnlyList<SlotSummary> slots, int preselectedSlot)
        {
            Slots = slots;
            Preselected = preselectedSlot;
        }
    }

    private class FakeSettingsView : FakeView, ISettingsView
    {
        public Action<float> MasterVolumeChanged;
        public Action<float> MusicVolumeChanged;
        public Action<float> SfxVolumeChanged;
        public Action<int> ResolutionChanged;
        public Action<int> DisplayModeChanged;
        public Action BackRequested;

        public SettingsViewState State { get; private set; }

        event Action<float> ISettingsView.MasterVolumeChanged { add => MasterVolumeChanged += value; remove => MasterVolumeChanged -= value; }
        event Action<float> ISettingsView.MusicVolumeChanged { add => MusicVolumeChanged += value; remove => MusicVolumeChanged -= value; }
        event Action<float> ISettingsView.SfxVolumeChanged { add => SfxVolumeChanged += value; remove => SfxVolumeChanged -= value; }
        event Action<int> ISettingsView.ResolutionChanged { add => ResolutionChanged += value; remove => ResolutionChanged -= value; }
        event Action<int> ISettingsView.DisplayModeChanged { add => DisplayModeChanged += value; remove => DisplayModeChanged -= value; }
        event Action ISettingsView.BackRequested { add => BackRequested += value; remove => BackRequested -= value; }

        public void SetState(SettingsViewState state) => State = state;
    }

    // Holds the question open until the test answers, like a real dialog waiting for the player.
    private class FakeConfirm : IConfirmDialog
    {
        private Action<bool> _onAnswer;

        public string LastMessage { get; private set; }
        public int Questions { get; private set; }

        public void Ask(string message, Action<bool> onAnswer)
        {
            Questions++;
            LastMessage = message;
            _onAnswer = onAnswer;
        }

        public void Answer(bool confirmed)
        {
            Action<bool> callback = _onAnswer;
            _onAnswer = null;
            callback?.Invoke(confirmed);
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
