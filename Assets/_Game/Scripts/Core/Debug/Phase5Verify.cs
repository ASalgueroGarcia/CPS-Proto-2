// TEMPORARY playtest-verification logging (Phase 5 structural branch, PR #41).
// Every call site carries the [Phase5] prefix and routes through this file - when
// the playtest gate passes, either flip ENABLED to false or delete this file and
// its call sites in one sweep (grep for "[Phase5]").
using UnityEngine;

public static class Phase5Verify
{
    public const bool Enabled = true;

    public static void Log(string message)
    {
        if (Enabled) Debug.Log($"[Phase5] {message}");
    }
}
