using System;

/// <summary>
/// What a slot card shows without loading the slot: empty, unreadable, or where the player
/// is (act / scene) and what they own. Read-only snapshot - views never touch SaveData.
/// </summary>
public class SlotSummary
{
    public int Slot { get; }
    public bool Exists { get; }

    /// <summary>The file is damaged or from a newer build; choosing the slot is still allowed.</summary>
    public bool Unreadable { get; }

    public int CurrentAct { get; }
    public int CurrentScene { get; }
    public int ScenesClearedTotal { get; }
    public int Tickets { get; }
    public int BossTickets { get; }

    /// <summary>Local time of the last save, or null when the slot has never been written.</summary>
    public DateTime? LastSaved { get; }

    private SlotSummary(int slot, bool exists, bool unreadable, SaveData data)
    {
        Slot = slot;
        Exists = exists;
        Unreadable = unreadable;
        if (data == null) return;

        CurrentAct = ProgressRules.DefaultAct(data);
        CurrentScene = ProgressRules.SceneToPlay(data, CurrentAct);
        for (int act = 0; act < ProgressRules.ActCount; act++)
        {
            ScenesClearedTotal += ProgressRules.GetScenesCleared(data, act);
        }

        Tickets = data.tickets;
        BossTickets = data.bossTickets;
        if (data.lastSavedUtcTicks > 0)
        {
            LastSaved = new DateTime(data.lastSavedUtcTicks, DateTimeKind.Utc).ToLocalTime();
        }
    }

    public static SlotSummary Empty(int slot) => new SlotSummary(slot, false, false, null);

    public static SlotSummary Damaged(int slot) => new SlotSummary(slot, true, true, null);

    public static SlotSummary From(int slot, SaveData data, bool unreadable = false) =>
        new SlotSummary(slot, true, unreadable, data);
}
