using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Everything one save slot keeps between runs (GDD > Gameplay > Save System).
/// Only progress that lives outside a run belongs here: what a run earns stays pending in
/// RunSession until the run ends, so quitting mid-run loses it.
/// Plain fields for JsonUtility - no properties, dictionaries or nested lists.
/// </summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    /// <summary>Persistent currency (GDD > Economy > Tickets).</summary>
    public int tickets;

    /// <summary>Persistent currency only a beaten boss pays (GDD > Economy > Boss Tickets).</summary>
    public int bossTickets;

    /// <summary>Lobby Merchant items bought. Bought items never come back to the stands.</summary>
    public List<string> ownedUpgradeIds = new List<string>();

    /// <summary>Per act, scenes beaten at least once (0-3). Decides what the poster offers.</summary>
    public int[] scenesCleared = new int[ProgressRules.ActCount];

    /// <summary>Per act, the scene PLAY AGAIN runs once the act is finished (0-2).</summary>
    public int[] replayScene = new int[ProgressRules.ActCount];

    /// <summary>Attempts at each scene since its last clear, indexed act * 3 + scene.</summary>
    public int[] takes = new int[ProgressRules.SceneCount];

    /// <summary>First-time setup has run for this slot.</summary>
    public bool firstLaunchDone;

    /// <summary>DateTime.UtcNow.Ticks of the last write, shown on the slot cards.</summary>
    public long lastSavedUtcTicks;

    public static SaveData CreateDefault() => new SaveData();

    /// <summary>
    /// Reads a slot file. Fields the file doesn't mention keep their defaults, so the format
    /// can grow without breaking old saves.
    /// </summary>
    public static SaveParseResult Parse(string json, out SaveData data)
    {
        data = CreateDefault();
        SaveParseResult result = SaveSerializer.TryOverwrite(json, data, CurrentVersion, out int fileVersion);
        if (result == SaveParseResult.Corrupt)
        {
            data = null;
            return result;
        }

        Upgrade(data, fileVersion);
        data.Normalize();
        return result;
    }

    /// <summary>
    /// Repairs whatever the file got wrong instead of trusting it: JsonUtility replaces arrays
    /// wholesale, so a short or hand-edited array would otherwise index out of range later.
    /// </summary>
    public void Normalize()
    {
        tickets = Mathf.Max(0, tickets);
        bossTickets = Mathf.Max(0, bossTickets);
        scenesCleared = Fit(scenesCleared, ProgressRules.ActCount, ProgressRules.ScenesPerAct);
        replayScene = Fit(replayScene, ProgressRules.ActCount, ProgressRules.ScenesPerAct - 1);
        takes = Fit(takes, ProgressRules.SceneCount, int.MaxValue);
        ownedUpgradeIds = Distinct(ownedUpgradeIds);
        // Outside DateTime's range the slot card would throw while drawing the menu.
        if (lastSavedUtcTicks < 0 || lastSavedUtcTicks > DateTime.MaxValue.Ticks) lastSavedUtcTicks = 0;
    }

    // Version 1 is the first format, so there is nothing to convert yet. A later format
    // change adds a step here, e.g. "if (fileVersion < 2) { ... }".
    private static void Upgrade(SaveData data, int fileVersion)
    {
        if (fileVersion <= CurrentVersion)
        {
            data.version = CurrentVersion;
        }
    }

    private static int[] Fit(int[] source, int length, int max)
    {
        var result = new int[length];
        if (source == null) return result;

        for (int i = 0; i < length && i < source.Length; i++)
        {
            result[i] = Mathf.Clamp(source[i], 0, max);
        }

        return result;
    }

    private static List<string> Distinct(List<string> ids)
    {
        var result = new List<string>();
        if (ids == null) return result;

        var seen = new HashSet<string>();
        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id) && seen.Add(id))
            {
                result.Add(id);
            }
        }

        return result;
    }
}
