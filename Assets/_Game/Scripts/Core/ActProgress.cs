using UnityEngine;

/// <summary>
/// Which act the player picked on the poster, and how far each act has got
/// (GDD: three acts, each played as three runs - the poster calls them "scenes").
/// Progress comes from the active save slot (ProgressRules over SaveData); only the poster
/// pick lives here, for the session. RunSession is the one place that writes progress.
/// </summary>
public static class ActProgress
{
    public const int ActCount = ProgressRules.ActCount;
    public const int ScenesPerAct = ProgressRules.ScenesPerAct;

    private static int _selectedAct = -1;

    private static SaveData Data => Services.Save?.Data;

    /// <summary>Act chosen on the poster, 0-based. Until the player picks one, the act they're working on.</summary>
    public static int SelectedAct =>
        _selectedAct >= 0 && IsUnlocked(_selectedAct) ? _selectedAct : ProgressRules.DefaultAct(Data);

    public static int GetScenesCleared(int act) => ProgressRules.GetScenesCleared(Data, act);

    public static bool IsFinished(int act) => ProgressRules.IsFinished(Data, act);

    /// <summary>Act I is always open; every later act opens once the one before it is finished.</summary>
    public static bool IsUnlocked(int act) => ProgressRules.IsUnlocked(Data, act);

    /// <summary>Scene (0-based) the next run of this act plays - a finished act replays from scene 1.</summary>
    public static int GetSceneToPlay(int act) => ProgressRules.SceneToPlay(Data, act);

    public static void SelectAct(int act)
    {
        if (!IsUnlocked(act))
        {
            Debug.LogWarning($"[ActProgress] Act {act + 1} is locked - selection ignored.");
            return;
        }

        _selectedAct = act;
    }

    // Statics survive entering Play Mode when domain reload is off - start every session clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAll()
    {
        _selectedAct = -1;
    }
}
