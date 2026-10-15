using NUnit.Framework;

/// <summary>
/// What a run does to the save (GDD > Save System): a save before the run, death keeps the
/// run's tickets and counts the take, victory clears the scene and resets the take, and a
/// quit leaves the slot exactly as it was before the run (Dima, 2026-10-15: take stays +0).
/// </summary>
public class RunSessionTests
{
    private TempSaveFolder _folder;
    private SaveService _save;
    private RunSession _run;

    [SetUp]
    public void SetUp()
    {
        _folder = new TempSaveFolder();
        _save = new SaveService(_folder.Store);
        _save.SelectSlot(0);
        _run = new RunSession(_save);
    }

    [TearDown]
    public void TearDown() => _folder.Dispose();

    /// <summary>The slot as the next launch would load it.</summary>
    private SaveData Disk()
    {
        var reloaded = new SaveService(_folder.Store);
        reloaded.SelectSlot(0);
        return reloaded.Data;
    }

    [Test]
    public void BeginRun_SavesBeforeTheRun_WithoutCountingTheTakeYet()
    {
        int saves = 0;
        _save.Saved += _ => saves++;

        _run.BeginRun(0);

        Assert.AreEqual(1, saves, "Every run starts with a save.");
        Assert.IsTrue(_run.IsActive);
        Assert.AreEqual(0, _run.Act);
        Assert.AreEqual(0, _run.Scene);
        Assert.AreEqual(1, _run.CurrentTake, "The map shows Take 1 during the first attempt.");
        Assert.AreEqual(0, Disk().takes[0]);
    }

    [Test]
    public void FirstRun_MarksFirstLaunchDone()
    {
        Assert.IsFalse(Disk().firstLaunchDone, "A new slot hasn't had its first-time setup yet.");

        _run.BeginRun(0);

        Assert.IsTrue(Disk().firstLaunchDone);
    }

    [Test]
    public void SwitchingSlotMidRun_NeverWritesTheRunIntoTheOtherSlot()
    {
        _run.BeginRun(0);
        _run.AddTickets(5);
        _save.SelectSlot(1);

        Assert.IsFalse(_run.EndRun(RunOutcome.Death));

        Assert.IsFalse(_run.IsActive);
        Assert.AreEqual(0, _save.Data.takes[0], "slot 2 untouched");
        Assert.AreEqual(0, _save.Data.tickets);
        Assert.AreEqual(0, Disk().takes[0], "slot 1 untouched (quit rules: take +0)");
    }

    [Test]
    public void Quit_LeavesTheSlotAsItWasBeforeTheRun()
    {
        _save.Data.tickets = 10;
        _save.Save();
        _run.BeginRun(0);
        _run.AddTickets(7);

        // The process ends here: no EndRun, no further save.

        SaveData disk = Disk();
        Assert.AreEqual(10, disk.tickets, "Tickets earned in the run are lost.");
        Assert.AreEqual(0, disk.takes[0], "The take isn't counted.");
    }

    [Test]
    public void Abandon_DropsTheRun_AndSavesNothing()
    {
        _run.BeginRun(0);
        _run.AddTickets(7);

        _run.AbandonRun();
        _save.Save();

        Assert.IsFalse(_run.IsActive);
        Assert.AreEqual(0, Disk().tickets);
        Assert.AreEqual(0, Disk().takes[0]);
    }

    [Test]
    public void Death_KeepsTheTickets_AndCountsTheTake()
    {
        _run.BeginRun(0);
        _run.AddTickets(4);
        _run.AddBossTickets(1);

        bool firstClear = _run.EndRun(RunOutcome.Death);

        SaveData disk = Disk();
        Assert.IsFalse(firstClear);
        Assert.AreEqual(4, disk.tickets);
        Assert.AreEqual(1, disk.bossTickets, "Boss Tickets are never at risk on death.");
        Assert.AreEqual(1, disk.takes[0]);
        Assert.AreEqual(0, disk.scenesCleared[0]);
        Assert.IsFalse(_run.IsActive);
    }

    [Test]
    public void Takes_AddUpAcrossDeaths()
    {
        for (int i = 0; i < 3; i++)
        {
            _run.BeginRun(0);
            _run.EndRun(RunOutcome.Death);
        }

        _run.BeginRun(0);
        Assert.AreEqual(4, _run.CurrentTake);
        Assert.AreEqual(3, Disk().takes[0]);
    }

    [Test]
    public void Victory_ClearsTheScene_PaysOut_AndResetsTheTake()
    {
        _run.BeginRun(0);
        _run.EndRun(RunOutcome.Death);
        _run.BeginRun(0);
        _run.AddTickets(5);

        bool firstClear = _run.EndRun(RunOutcome.Victory);

        SaveData disk = Disk();
        Assert.IsTrue(firstClear);
        Assert.AreEqual(1, disk.scenesCleared[0]);
        Assert.AreEqual(0, disk.takes[0]);
        Assert.AreEqual(5, disk.tickets);
        Assert.AreEqual(RunSession.BossTicketsPerVictory, disk.bossTickets);
    }

    [Test]
    public void ClearingTheThirdScene_UnlocksTheNextAct()
    {
        for (int scene = 0; scene < ProgressRules.ScenesPerAct; scene++)
        {
            _run.BeginRun(0);
            Assert.AreEqual(scene, _run.Scene);
            _run.EndRun(RunOutcome.Victory);
        }

        Assert.IsTrue(ProgressRules.IsFinished(Disk(), 0));
        Assert.IsTrue(ProgressRules.IsUnlocked(Disk(), 1));
    }

    [Test]
    public void PlayAgain_ReplaysTheActFromSceneOne_InOrder()
    {
        _save.Data.scenesCleared[0] = ProgressRules.ScenesPerAct;
        _save.Save();

        int[] played = new int[4];
        for (int i = 0; i < played.Length; i++)
        {
            _run.BeginRun(0);
            played[i] = _run.Scene;
            Assert.IsFalse(_run.EndRun(RunOutcome.Victory), "A replay is never a first clear.");
        }

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, played);
        Assert.AreEqual(ProgressRules.ScenesPerAct, Disk().scenesCleared[0]);
    }

    [Test]
    public void DyingInAReplay_KeepsTheReplayOnTheSameScene()
    {
        _save.Data.scenesCleared[0] = ProgressRules.ScenesPerAct;
        _save.Data.replayScene[0] = 1;
        _save.Save();

        _run.BeginRun(0);
        _run.EndRun(RunOutcome.Death);

        Assert.AreEqual(1, Disk().replayScene[0]);
        Assert.AreEqual(1, Disk().takes[ProgressRules.TakeIndex(0, 1)]);
    }

    [Test]
    public void EndRun_WithoutARun_DoesNothing()
    {
        Assert.IsFalse(_run.EndRun(RunOutcome.Victory));
        Assert.AreEqual(0, Disk().scenesCleared[0]);
        Assert.AreEqual(0, Disk().bossTickets);
    }

    [Test]
    public void EndRun_Twice_CountsOnce()
    {
        _run.BeginRun(0);
        _run.EndRun(RunOutcome.Death);
        _run.EndRun(RunOutcome.Death);

        Assert.AreEqual(1, Disk().takes[0]);
    }

    [Test]
    public void BeginRun_WhileARunIsActive_AbandonsTheFirst()
    {
        _run.BeginRun(0);
        _run.AddTickets(9);

        _run.BeginRun(0);
        _run.EndRun(RunOutcome.Death);

        Assert.AreEqual(0, Disk().tickets, "The first run's tickets were never committed.");
        Assert.AreEqual(1, Disk().takes[0]);
    }

    [Test]
    public void LockedAct_FallsBackToTheActBeingWorkedOn()
    {
        _run.BeginRun(2);

        Assert.AreEqual(0, _run.Act);
    }

    [Test]
    public void Tickets_OutsideARun_AreIgnored()
    {
        _run.AddTickets(5);
        _run.BeginRun(0);
        _run.EndRun(RunOutcome.Death);

        Assert.AreEqual(0, Disk().tickets);
    }
}
