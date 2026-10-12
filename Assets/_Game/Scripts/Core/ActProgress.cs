using UnityEngine;

/// <summary>
/// Which act the player picked on the poster, and how far each act has got
/// (GDD: three acts, each played as three runs - the poster calls them "scenes").
/// Store only: the act select screen writes the choice, the map / run code reads it.
/// There is no save system yet, so progress lives for the session only.
/// </summary>
public static class ActProgress
{
    public const int ActCount = 3;
    public const int ScenesPerAct = 3;

    private static readonly int[] ScenesCleared = new int[ActCount];

    /// <summary>Act chosen on the poster, 0-based. Act I until the player picks one.</summary>
    public static int SelectedAct { get; private set; }

    public static int GetScenesCleared(int act) => IsValid(act) ? ScenesCleared[act] : 0;

    public static bool IsFinished(int act) => GetScenesCleared(act) >= ScenesPerAct;

    /// <summary>Act I is always open; every later act opens once the one before it is finished.</summary>
    public static bool IsUnlocked(int act)
    {
        if (!IsValid(act)) return false;
        return act == 0 || IsFinished(act - 1);
    }

    public static void SelectAct(int act)
    {
        if (!IsUnlocked(act))
        {
            Debug.LogWarning($"[ActProgress] Act {act + 1} is locked - selection ignored.");
            return;
        }

        SelectedAct = act;
    }

    /// <summary>Call when a run of the selected act is won (its Mini-Boss / Act Boss is down).</summary>
    public static void MarkSceneCleared()
    {
        if (ScenesCleared[SelectedAct] < ScenesPerAct)
        {
            ScenesCleared[SelectedAct]++;
        }
    }

    // Statics survive entering Play Mode when domain reload is off - start every session clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAll()
    {
        System.Array.Clear(ScenesCleared, 0, ActCount);
        SelectedAct = 0;
    }

    private static bool IsValid(int act) => act >= 0 && act < ActCount;
}
