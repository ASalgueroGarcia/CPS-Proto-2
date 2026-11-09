using NUnit.Framework;

/// <summary>The poster's ActProgress reads the active save slot through Services.</summary>
public class ActProgressTests
{
    private TempSaveFolder _folder;
    private SaveService _save;

    [SetUp]
    public void SetUp()
    {
        _folder = new TempSaveFolder();
        _save = new SaveService(_folder.Store);
        Services.Install(_save, new SettingsService(_folder.Store), new RunSession(_save));
        ActProgress.ResetAll();
    }

    [TearDown]
    public void TearDown()
    {
        Services.ResetForTests();
        ActProgress.ResetAll();
        _folder.Dispose();
    }

    [Test]
    public void Progress_ComesFromTheActiveSlot()
    {
        _save.SelectSlot(0);
        _save.Data.scenesCleared[0] = 3;

        Assert.IsTrue(ActProgress.IsFinished(0));
        Assert.IsTrue(ActProgress.IsUnlocked(1));
        Assert.AreEqual(3, ActProgress.GetScenesCleared(0));
        Assert.AreEqual(0, ActProgress.GetSceneToPlay(0), "PLAY AGAIN starts the act over.");

        _save.SelectSlot(1);
        Assert.IsFalse(ActProgress.IsUnlocked(1));
    }

    [Test]
    public void SelectedAct_DefaultsToTheActBeingWorkedOn()
    {
        _save.SelectSlot(0);
        _save.Data.scenesCleared[0] = 3;

        Assert.AreEqual(1, ActProgress.SelectedAct);
    }

    [Test]
    public void PickingAnUnfinishedAct_LastsUntilItIsFinished()
    {
        _save.SelectSlot(0);
        ActProgress.SelectAct(0);

        _save.Data.scenesCleared[0] = 3;

        Assert.AreEqual(1, ActProgress.SelectedAct, "After the third clear the next run moves on to Act II.");
    }

    [Test]
    public void APick_BelongsToItsSlot()
    {
        // Act I finished in both slots, so the pick is valid in either - only the slot tells them apart.
        _save.SelectSlot(1);
        _save.Data.scenesCleared[0] = 3;
        _save.Save();
        _save.SelectSlot(0);
        _save.Data.scenesCleared[0] = 3;
        _save.Save();
        ActProgress.SelectAct(0);
        Assert.AreEqual(0, ActProgress.SelectedAct, "PLAY AGAIN on Act I in slot 1.");

        _save.SelectSlot(1);

        Assert.AreEqual(1, ActProgress.SelectedAct, "Slot 2 goes on with Act II - slot 1's replay isn't its pick.");
    }

    [Test]
    public void SelectingALockedAct_IsIgnored_ButAFinishedOneCanBePickedAgain()
    {
        _save.SelectSlot(0);

        ActProgress.SelectAct(2);
        Assert.AreEqual(0, ActProgress.SelectedAct);

        _save.Data.scenesCleared[0] = 3;
        _save.Data.scenesCleared[1] = 3;
        ActProgress.SelectAct(0);
        Assert.AreEqual(0, ActProgress.SelectedAct);
    }
}
