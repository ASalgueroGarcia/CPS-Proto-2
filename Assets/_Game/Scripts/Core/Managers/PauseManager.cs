// SINGLE OWNER of the game's paused state (Phase 3 of the cleanup audit).
// Before this class, UIManager, ShopManager and MapBtnBehaviour each wrote
// Time.timeScale and/or PlayerFSM.IsPaused independently - the shop froze
// timeScale without blocking combat input, which is exactly the drift this
// prevents by writing both representations atomically in one place.
// All game-flow pause changes route through SetPaused. SetTimeScale exists
// for dev speed control only. PlayerFSM.IsPaused stays as the legacy
// combat-input flag until Phase 5 migrates its readers.
using UnityEngine;

public static class PauseManager
{
    public static bool IsPaused { get; private set; }

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        PlayerFSM.IsPaused = paused;
    }

    public static void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
    }
}
