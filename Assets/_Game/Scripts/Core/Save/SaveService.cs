using System;
using System.IO;
using UnityEngine;

/// <summary>
/// The three save slots (GDD > Save System: automatic saves, each slot holds only its latest
/// state). One slot is active at a time; everything else reads it through Data.
/// Loading falls back main file -> .bak -> fresh defaults, and a slot that can't be read
/// safely (newer build, locked file) turns read-only so a good save is never overwritten.
/// </summary>
public class SaveService
{
    public const int SlotCount = 3;

    private enum FileState { Missing, Ok, Corrupt, NewerVersion, IoError }

    private readonly JsonFileStore _store;
    private readonly bool[] _readOnly = new bool[SlotCount];
    private readonly bool[] _readOnlyWarned = new bool[SlotCount];

    /// <summary>Raised after a slot file was written.</summary>
    public event Action<int> Saved;

    /// <summary>Raised when a write failed or was refused; the second argument says why.</summary>
    public event Action<int, string> SaveFailed;

    /// <summary>Raised with the new slot, or -1 when the active slot was deleted.</summary>
    public event Action<int> ActiveSlotChanged;

    public int ActiveSlot { get; private set; } = -1;
    public bool HasActiveSlot => ActiveSlot >= 0;

    /// <summary>The active slot's progress, or null when no slot is active.</summary>
    public SaveData Data { get; private set; }

    public bool IsReadOnly => HasActiveSlot && _readOnly[ActiveSlot];

    /// <summary>
    /// Set by RunSession. Lobby purchases are refused during a run, so a mid-run save can only
    /// ever write what the slot held when the run started.
    /// </summary>
    public bool RunInProgress { get; internal set; }

    public SaveService(JsonFileStore store)
    {
        _store = store;
    }

    public static string FileNameOf(int slot) => $"save_{slot}.json";

    public static bool IsValidSlot(int slot) => slot >= 0 && slot < SlotCount;

    /// <summary>Reads a slot for its card without making it active or repairing anything.</summary>
    public SlotSummary Peek(int slot)
    {
        if (!IsValidSlot(slot)) return SlotSummary.Empty(slot);
        if (slot == ActiveSlot) return SlotSummary.From(slot, Data, _readOnly[slot]);

        string main = FileNameOf(slot);
        FileState state = ReadFile(main, out SaveData data);
        if (state == FileState.Missing || state == FileState.Corrupt)
        {
            FileState backupState = ReadFile(main + JsonFileStore.BackupSuffix, out SaveData backup);
            if (backupState == FileState.Ok) return SlotSummary.From(slot, backup);
        }

        switch (state)
        {
            case FileState.Missing: return SlotSummary.Empty(slot);
            case FileState.Ok: return SlotSummary.From(slot, data);
            case FileState.NewerVersion: return SlotSummary.From(slot, data, true);
            case FileState.IoError: return SlotSummary.Unavailable(slot);
            default: return SlotSummary.Damaged(slot);
        }
    }

    /// <summary>Makes the slot active, creating it at starting values if it doesn't exist yet.</summary>
    public void SelectSlot(int slot)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogWarning($"[SaveService] There is no save slot {slot}.");
            return;
        }

        Data = LoadSlot(slot, out bool needsWrite);
        ActiveSlot = slot;
        if (needsWrite)
        {
            Save();
        }

        ActiveSlotChanged?.Invoke(slot);
    }

    /// <summary>Deletes the slot's file and backup. Refused for the slot a run is using.</summary>
    public bool DeleteSlot(int slot)
    {
        if (!IsValidSlot(slot)) return false;
        if (RunInProgress && slot == ActiveSlot)
        {
            Debug.LogWarning("[SaveService] Can't delete the slot of the run in progress.");
            return false;
        }

        try
        {
            _store.Delete(FileNameOf(slot));
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[SaveService] Could not delete save slot {slot}: {e.Message}");
            return false;
        }

        _readOnly[slot] = false;
        if (slot == ActiveSlot)
        {
            ActiveSlot = -1;
            Data = null;
            ActiveSlotChanged?.Invoke(-1);
        }

        return true;
    }

    /// <summary>Writes the active slot. Returns false when nothing could be written.</summary>
    public bool Save()
    {
        if (!HasActiveSlot) return false;

        int slot = ActiveSlot;
        if (_readOnly[slot])
        {
            if (!_readOnlyWarned[slot])
            {
                _readOnlyWarned[slot] = true;
                Debug.LogWarning($"[SaveService] Save slot {slot} is read-only this session - progress is not being saved.");
            }

            SaveFailed?.Invoke(slot, "The slot is read-only this session (newer save or unreadable file).");
            return false;
        }

        Data.lastSavedUtcTicks = DateTime.UtcNow.Ticks;
        try
        {
            _store.WriteAtomic(FileNameOf(slot), SaveSerializer.ToJson(Data));
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogError($"[SaveService] Saving slot {slot} failed: {e.Message}");
            SaveFailed?.Invoke(slot, e.Message);
            return false;
        }

        Saved?.Invoke(slot);
        return true;
    }

    public bool OwnsUpgrade(string upgradeId) =>
        HasActiveSlot && !string.IsNullOrWhiteSpace(upgradeId) && Data.ownedUpgradeIds.Contains(upgradeId);

    /// <summary>
    /// Lobby Merchant purchase: pays, records the item for good and saves straight away
    /// (GDD > Save System: "Save written immediately after each purchase").
    /// </summary>
    public bool TryPurchaseUpgrade(string upgradeId, int ticketCost, int bossTicketCost)
    {
        if (!HasActiveSlot || RunInProgress || IsReadOnly) return false;
        if (string.IsNullOrWhiteSpace(upgradeId) || OwnsUpgrade(upgradeId)) return false;
        if (ticketCost < 0 || bossTicketCost < 0) return false;
        if (Data.tickets < ticketCost || Data.bossTickets < bossTicketCost) return false;

        Data.tickets -= ticketCost;
        Data.bossTickets -= bossTicketCost;
        Data.ownedUpgradeIds.Add(upgradeId);
        if (Save()) return true;

        // Not on disk means not bought: the next launch would hand the item back for free.
        Data.tickets += ticketCost;
        Data.bossTickets += bossTicketCost;
        Data.ownedUpgradeIds.Remove(upgradeId);
        return false;
    }

    public void MarkFirstLaunchDone()
    {
        if (!HasActiveSlot || Data.firstLaunchDone) return;

        Data.firstLaunchDone = true;
        Save();
    }

    private SaveData LoadSlot(int slot, out bool needsWrite)
    {
        string main = FileNameOf(slot);
        string backup = main + JsonFileStore.BackupSuffix;
        _readOnly[slot] = false;
        _readOnlyWarned[slot] = false;
        needsWrite = false;

        FileState mainState = ReadFile(main, out SaveData data);
        if (mainState == FileState.Ok) return data;
        if (mainState == FileState.NewerVersion || mainState == FileState.IoError)
        {
            return OpenReadOnly(slot, data);
        }

        // The main file is missing or damaged: the backup holds the write before it.
        FileState backupState = ReadFile(backup, out SaveData backupData);
        if (mainState == FileState.Corrupt)
        {
            TryQuarantine(main);
        }

        needsWrite = true;
        switch (backupState)
        {
            case FileState.Ok:
                Debug.LogWarning($"[SaveService] Save slot {slot} was restored from its backup.");
                return backupData;
            case FileState.NewerVersion:
            case FileState.IoError:
                needsWrite = false;
                return OpenReadOnly(slot, backupData);
            case FileState.Corrupt:
                TryQuarantine(backup);
                Debug.LogWarning($"[SaveService] Save slot {slot} was unreadable and starts over.");
                return SaveData.CreateDefault();
            default:
                if (mainState == FileState.Corrupt)
                {
                    Debug.LogWarning($"[SaveService] Save slot {slot} was unreadable and starts over.");
                }

                return SaveData.CreateDefault();
        }
    }

    private SaveData OpenReadOnly(int slot, SaveData data)
    {
        _readOnly[slot] = true;
        Debug.LogWarning($"[SaveService] Save slot {slot} can't be read safely - it won't be written this session.");
        return data ?? SaveData.CreateDefault();
    }

    private FileState ReadFile(string fileName, out SaveData data)
    {
        data = null;
        if (!_store.Exists(fileName)) return FileState.Missing;

        string json;
        try
        {
            json = _store.Read(fileName);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[SaveService] Could not read {fileName}: {e.Message}");
            return FileState.IoError;
        }

        switch (SaveData.Parse(json, out data))
        {
            case SaveParseResult.Ok: return FileState.Ok;
            case SaveParseResult.NewerVersion: return FileState.NewerVersion;
            default: return FileState.Corrupt;
        }
    }

    private void TryQuarantine(string fileName)
    {
        try
        {
            _store.Quarantine(fileName);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogWarning($"[SaveService] Could not move {fileName} aside: {e.Message}");
        }
    }
}
