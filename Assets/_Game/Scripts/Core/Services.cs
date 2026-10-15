using UnityEngine;

/// <summary>
/// The save, settings and run services in one place. Built before the first scene loads, so
/// every scene - also one played straight from the editor - finds them ready, with no manager
/// object to place. Flow is registered by the UI layer (Core can't see it).
/// </summary>
public static class Services
{
    public static SaveService Save { get; private set; }
    public static SettingsService Settings { get; private set; }
    public static RunSession Run { get; private set; }
    public static IGameFlow Flow { get; private set; }

    public static void RegisterFlow(IGameFlow flow)
    {
        Flow = flow;
    }

    /// <summary>Wires services built elsewhere (tests use a temp folder instead of persistentDataPath).</summary>
    public static void Install(SaveService save, SettingsService settings, RunSession run)
    {
        Save = save;
        Settings = settings;
        Run = run;
    }

    public static void ResetForTests() => ResetStatics();

    // Statics survive entering Play Mode when domain reload is off - start every session clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Save = null;
        Settings = null;
        Run = null;
        Flow = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var store = new JsonFileStore(Application.persistentDataPath);

        var settings = new SettingsService(store);
        settings.Load();
        settings.DisplayChanged += DisplaySettings.Apply;
        DisplaySettings.Apply(settings.Data);

        var save = new SaveService(store);
        save.ActiveSlotChanged += slot => RememberSlot(settings, slot);

        Install(save, settings, new RunSession(save));

        // Reopen the last slot only if it exists and reads cleanly: selecting a missing slot would create
        // it, and selecting a damaged one would repair it before the player ever saw "DAMAGED SAVE".
        int lastSlot = settings.Data.lastSlot;
        if (SaveService.IsValidSlot(lastSlot))
        {
            SlotSummary last = save.Peek(lastSlot);
            if (last.Exists && !last.Unreadable) save.SelectSlot(lastSlot);
        }
    }

    private static void RememberSlot(SettingsService settings, int slot)
    {
        if (slot < 0) return;

        settings.SetLastSlot(slot);
        settings.Save();
    }
}
