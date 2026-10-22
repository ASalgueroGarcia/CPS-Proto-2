using System;
using System.Globalization;
using NUnit.Framework;

/// <summary>What a save slot card says, for every state a slot can be in. Shared by all slot views.</summary>
public class SlotCardTextTests
{
    private static SaveData Progress()
    {
        SaveData data = SaveData.CreateDefault();
        data.scenesCleared[0] = 3;
        data.scenesCleared[1] = 1;
        data.tickets = 24;
        data.bossTickets = 3;
        data.lastSavedUtcTicks = new DateTime(2026, 10, 15, 12, 40, 0, DateTimeKind.Utc).Ticks;
        return data;
    }

    [Test]
    public void EmptySlot_IsANewGame()
    {
        Assert.AreEqual("SAVE 2", SlotCardText.Title(SlotSummary.Empty(1)));
        Assert.AreEqual("NEW GAME", SlotCardText.Details(SlotSummary.Empty(1)));
    }

    [Test]
    public void SlotWithProgress_ShowsWhereThePlayerIs()
    {
        string text = SlotCardText.Details(SlotSummary.From(0, Progress()));

        StringAssert.StartsWith("ACT II · SCENE 2\n24 Tickets · 3 Boss Tickets\n", text);
        StringAssert.DoesNotContain("READ-ONLY", text);
    }

    [Test]
    public void DamagedAndLockedSlots_SayDifferentThings()
    {
        StringAssert.StartsWith("DAMAGED SAVE", SlotCardText.Details(SlotSummary.Damaged(0)));
        StringAssert.StartsWith("CAN'T OPEN THIS SAVE", SlotCardText.Details(SlotSummary.Unavailable(0)));
    }

    [Test]
    public void SaveFromANewerBuild_IsMarkedReadOnly()
    {
        StringAssert.EndsWith("READ-ONLY", SlotCardText.Details(SlotSummary.From(0, Progress(), true)));
    }

    [Test]
    public void Date_DoesNotDependOnTheWindowsLanguage()
    {
        CultureInfo before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("es-ES");
            string text = SlotCardText.Details(SlotSummary.From(0, Progress()));
            StringAssert.Contains(" Oct 2026", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}
