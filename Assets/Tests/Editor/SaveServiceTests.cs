using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// The three slots: isolated from each other, created on first use, recovered from the backup
/// when the main file is damaged, and never overwritten when they can't be read safely.
/// </summary>
public class SaveServiceTests
{
    private TempSaveFolder _folder;
    private SaveService _save;

    [SetUp]
    public void SetUp()
    {
        _folder = new TempSaveFolder();
        _save = new SaveService(_folder.Store);
    }

    [TearDown]
    public void TearDown() => _folder.Dispose();

    private static string Slot(int slot) => SaveService.FileNameOf(slot);

    private SaveData ReadFromDisk(int slot)
    {
        Assert.AreEqual(SaveParseResult.Ok, SaveData.Parse(_folder.ReadText(Slot(slot)), out SaveData data));
        return data;
    }

    [Test]
    public void NoSlotIsActive_UntilOneIsSelected()
    {
        Assert.IsFalse(_save.HasActiveSlot);
        Assert.IsNull(_save.Data);
        Assert.IsFalse(_save.Save());
    }

    [Test]
    public void SelectingAnEmptySlot_CreatesItAtStartingValues()
    {
        _save.SelectSlot(1);

        Assert.AreEqual(1, _save.ActiveSlot);
        Assert.IsTrue(_folder.Exists(Slot(1)));
        Assert.AreEqual(0, ReadFromDisk(1).tickets);
        Assert.IsFalse(ReadFromDisk(1).firstLaunchDone);
    }

    [Test]
    public void Slots_AreIndependent()
    {
        _save.SelectSlot(1);
        _save.Data.tickets = 40;
        _save.Save();

        Assert.IsFalse(_folder.Exists(Slot(0)));
        Assert.IsFalse(_folder.Exists(Slot(2)));

        _save.SelectSlot(2);
        Assert.AreEqual(0, _save.Data.tickets);

        _save.SelectSlot(1);
        Assert.AreEqual(40, _save.Data.tickets);
    }

    [Test]
    public void Peek_ReadsASlot_WithoutSelectingOrCreatingIt()
    {
        _save.SelectSlot(0);
        _save.Data.tickets = 12;
        _save.Data.scenesCleared[0] = 2;
        _save.Save();
        _save.SelectSlot(2);

        SlotSummary first = _save.Peek(0);
        SlotSummary empty = _save.Peek(1);

        Assert.AreEqual(2, _save.ActiveSlot);
        Assert.IsTrue(first.Exists);
        Assert.AreEqual(12, first.Tickets);
        Assert.AreEqual(0, first.CurrentAct);
        Assert.AreEqual(2, first.CurrentScene);
        Assert.AreEqual(2, first.ScenesClearedTotal);
        Assert.IsNotNull(first.LastSaved);
        Assert.IsFalse(empty.Exists);
        Assert.IsFalse(_folder.Exists(Slot(1)));
    }

    [Test]
    public void DeletingTheActiveSlot_LeavesNoActiveSlot()
    {
        _save.SelectSlot(0);
        _save.Save();
        int changedTo = 99;
        _save.ActiveSlotChanged += slot => changedTo = slot;

        Assert.IsTrue(_save.DeleteSlot(0));

        Assert.IsFalse(_save.HasActiveSlot);
        Assert.AreEqual(-1, changedTo);
        Assert.IsEmpty(_folder.Files());
        Assert.IsFalse(_save.Peek(0).Exists);
    }

    [Test]
    public void DamagedMainFile_IsRestoredFromTheBackup()
    {
        _save.SelectSlot(0);
        _save.Data.tickets = 5;
        _save.Save();
        _save.Data.tickets = 6;
        _save.Save();
        _folder.Write(Slot(0), "{ broken");

        var reloaded = new SaveService(_folder.Store);
        reloaded.SelectSlot(0);

        Assert.AreEqual(5, reloaded.Data.tickets, "The backup holds the write before the damaged one.");
        Assert.AreEqual(5, ReadFromDisk(0).tickets, "The restored state is written back as the main file.");
        Assert.IsTrue(_folder.Files().Any(f => f.Contains(".corrupt-")), "The damaged file is kept aside.");
    }

    [Test]
    public void DamagedMainAndBackup_StartOverAndKeepBothAside()
    {
        _folder.Write(Slot(0), "null");
        _folder.Write(Slot(0) + JsonFileStore.BackupSuffix, "[]");

        _save.SelectSlot(0);

        Assert.AreEqual(0, _save.Data.tickets);
        Assert.AreEqual(SaveParseResult.Ok, SaveData.Parse(_folder.ReadText(Slot(0)), out _));
        Assert.AreEqual(2, _folder.Files().Count(f => f.Contains(".corrupt-")));
    }

    [Test]
    public void DamagedSlot_ShowsAsUnreadableOnItsCard()
    {
        _folder.Write(Slot(2), "garbage");

        SlotSummary card = _save.Peek(2);

        Assert.IsTrue(card.Exists);
        Assert.IsTrue(card.Unreadable);
    }

    [Test]
    public void SaveFromANewerBuild_IsNeverOverwritten()
    {
        string newer = "{\"version\":" + (SaveData.CurrentVersion + 1) + ",\"tickets\":9}";
        _folder.Write(Slot(0), newer);
        string failure = null;
        _save.SaveFailed += (slot, reason) => failure = reason;

        _save.SelectSlot(0);
        _save.Data.tickets = 1;

        Assert.IsTrue(_save.IsReadOnly);
        Assert.IsTrue(_save.Peek(0).Unreadable);
        Assert.IsFalse(_save.Save());
        Assert.IsNotNull(failure);
        Assert.AreEqual(newer, _folder.ReadText(Slot(0)));
    }

    [Test]
    public void LockedFile_ReportsTheFailure_WithoutThrowing()
    {
        _save.SelectSlot(0);
        string failure = null;
        _save.SaveFailed += (slot, reason) => failure = reason;

        using (new FileStream(_folder.FilePath(Slot(0)), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveService\] Saving slot 0 failed"));
            Assert.IsFalse(_save.Save());
        }

        Assert.IsNotNull(failure);
        Assert.IsTrue(_save.Save(), "A failed write doesn't block later ones once the file is free.");
    }

    [Test]
    public void Purchase_PaysRecordsAndSavesAtOnce()
    {
        _save.SelectSlot(0);
        _save.Data.tickets = 10;
        _save.Data.bossTickets = 2;

        Assert.IsTrue(_save.TryPurchaseUpgrade("spare_stuffing", 6, 1));

        Assert.AreEqual(4, _save.Data.tickets);
        Assert.AreEqual(1, _save.Data.bossTickets);
        Assert.IsTrue(_save.OwnsUpgrade("spare_stuffing"));
        SaveData disk = ReadFromDisk(0);
        Assert.AreEqual(4, disk.tickets);
        CollectionAssert.Contains(disk.ownedUpgradeIds, "spare_stuffing");
    }

    [Test]
    public void Purchase_IsRefused_WithoutTheMoney_OrWhenAlreadyOwned()
    {
        _save.SelectSlot(0);
        _save.Data.tickets = 5;

        Assert.IsFalse(_save.TryPurchaseUpgrade("needle", 6, 0), "Not enough Tickets.");
        Assert.IsFalse(_save.TryPurchaseUpgrade("needle", 0, 1), "Not enough Boss Tickets.");
        Assert.IsTrue(_save.TryPurchaseUpgrade("needle", 5, 0));
        Assert.IsFalse(_save.TryPurchaseUpgrade("needle", 0, 0), "Bought items never come back.");
        Assert.AreEqual(0, _save.Data.tickets);
    }

    [Test]
    public void Purchase_AndDelete_AreRefused_DuringARun()
    {
        _save.SelectSlot(0);
        _save.Data.tickets = 50;
        var run = new RunSession(_save);
        run.BeginRun(0);

        Assert.IsFalse(_save.TryPurchaseUpgrade("needle", 1, 0));
        Assert.IsFalse(_save.DeleteSlot(0), "The run's slot can't be deleted under it.");
    }

    [Test]
    public void Purchase_ThatCannotBeSaved_IsRolledBack()
    {
        _save.SelectSlot(0);
        _save.Data.tickets = 10;
        _save.Save();

        using (new FileStream(_folder.FilePath(Slot(0)), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            LogAssert.Expect(LogType.Error, new Regex(@"\[SaveService\] Saving slot 0 failed"));
            Assert.IsFalse(_save.TryPurchaseUpgrade("needle", 6, 0));
        }

        Assert.AreEqual(10, _save.Data.tickets, "Not on disk means not bought.");
        Assert.IsFalse(_save.OwnsUpgrade("needle"));
    }

    [Test]
    public void LockedSlot_ShowsAsUnavailable_NotDamaged()
    {
        _save.SelectSlot(0);
        _save.SelectSlot(1);

        using (new FileStream(_folder.FilePath(Slot(0)), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            SlotSummary card = _save.Peek(0);

            Assert.IsTrue(card.Exists);
            Assert.IsTrue(card.Locked);
        }

        Assert.IsFalse(_save.Peek(0).Locked, "Readable again once the other program lets go.");
    }
}
