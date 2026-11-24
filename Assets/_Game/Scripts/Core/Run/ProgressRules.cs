using UnityEngine;

/// <summary>
/// The act / scene rules (GDD: three acts, each played as three runs - the poster calls
/// them "scenes"). Pure functions over a slot's SaveData, so the poster, the run and the
/// tests all read progress the same way. A null SaveData reads as a fresh save.
/// </summary>
public static class ProgressRules
{
    public const int ActCount = 3;
    public const int ScenesPerAct = 3;
    public const int SceneCount = ActCount * ScenesPerAct;

    public static bool IsValidAct(int act) => act >= 0 && act < ActCount;

    public static int GetScenesCleared(SaveData data, int act)
    {
        if (data == null || !IsValidAct(act)) return 0;
        return data.scenesCleared[act];
    }

    public static bool IsFinished(SaveData data, int act) => GetScenesCleared(data, act) >= ScenesPerAct;

    /// <summary>Act I is always open; every later act opens once the one before it is finished.</summary>
    public static bool IsUnlocked(SaveData data, int act)
    {
        if (!IsValidAct(act)) return false;
        return act == 0 || IsFinished(data, act - 1);
    }

    /// <summary>
    /// Scene (0-based) the next run of this act plays: the first one not cleared yet. A finished
    /// act is replayed from the beginning (PLAY AGAIN), walking its scenes in order again.
    /// </summary>
    public static int SceneToPlay(SaveData data, int act)
    {
        if (data == null || !IsValidAct(act)) return 0;
        if (IsFinished(data, act)) return data.replayScene[act];
        return Mathf.Clamp(data.scenesCleared[act], 0, ScenesPerAct - 1);
    }

    /// <summary>The act the player is working on: the first unlocked one not finished yet.</summary>
    public static int DefaultAct(SaveData data)
    {
        for (int act = 0; act < ActCount; act++)
        {
            if (IsUnlocked(data, act) && !IsFinished(data, act)) return act;
        }

        return ActCount - 1;
    }

    public static int TakeIndex(int act, int scene) => act * ScenesPerAct + scene;
}
