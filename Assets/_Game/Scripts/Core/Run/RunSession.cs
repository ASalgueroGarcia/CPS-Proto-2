using System;
using UnityEngine;

/// <summary>
/// One run, from the poster to death or victory, and what it does to the save
/// (GDD > Save System: every run starts with a save and every finished run ends with one).
/// What the run earns - tickets and the attempt itself - stays pending here until the run
/// ends. Quitting or crashing mid-run skips that last save, so the player is back at the
/// pre-run state: run tickets lost, take not counted.
/// </summary>
public class RunSession
{
    /// <summary>
    /// Placeholder: the GDD (Rewards) says the Final Fight pays Boss Tickets but not how many.
    /// Replace with the design value once Economy defines it.
    /// </summary>
    public const int BossTicketsPerVictory = 1;

    private readonly SaveService _save;
    private int _slot = -1;
    private int _pendingTickets;
    private int _pendingBossTickets;

    /// <summary>Raised after the end-of-run save. The bool is true on a scene's first clear (story beats hang off it).</summary>
    public event Action<RunOutcome, bool> RunEnded;

    public bool IsActive { get; private set; }

    /// <summary>Act (0-based) of the run in progress.</summary>
    public int Act { get; private set; }

    /// <summary>Scene (0-based) of the run in progress.</summary>
    public int Scene { get; private set; }

    public int PendingTickets => _pendingTickets;
    public int PendingBossTickets => _pendingBossTickets;

    /// <summary>The "Take N" of the run in progress: saved attempts plus this one. 0 when no run is active.</summary>
    public int CurrentTake =>
        IsActive && _save.HasActiveSlot ? _save.Data.takes[ProgressRules.TakeIndex(Act, Scene)] + 1 : 0;

    public RunSession(SaveService save)
    {
        _save = save;
    }

    /// <summary>
    /// Starts a run of <paramref name="act"/> at the scene the rules pick, and saves before the
    /// run loads. A locked act falls back to the act the player is working on.
    /// </summary>
    public void BeginRun(int act)
    {
        if (IsActive)
        {
            AbandonRun();
        }

        if (!_save.HasActiveSlot)
        {
            // Only happens when a scene is played without going through the slot screen.
            Debug.LogWarning("[RunSession] No save slot chosen - using slot 1.");
            _save.SelectSlot(0);
        }

        SaveData data = _save.Data;
        if (!ProgressRules.IsUnlocked(data, act))
        {
            Debug.LogWarning($"[RunSession] Act {act + 1} is locked - starting the current act instead.");
            act = ProgressRules.DefaultAct(data);
        }

        Act = act;
        Scene = ProgressRules.SceneToPlay(data, act);
        _slot = _save.ActiveSlot;
        _pendingTickets = 0;
        _pendingBossTickets = 0;
        IsActive = true;
        _save.RunInProgress = true;

        // The first run marks the slot's first-time setup as done; a lobby intro can read the flag before it.
        data.firstLaunchDone = true;
        _save.Save();
    }

    /// <summary>Tickets earned during the run. They only reach the save when the run ends.</summary>
    public void AddTickets(int amount)
    {
        if (IsActive && amount > 0) _pendingTickets += amount;
    }

    public void AddBossTickets(int amount)
    {
        if (IsActive && amount > 0) _pendingBossTickets += amount;
    }

    /// <summary>
    /// Death: keeps the run's tickets and counts the take. Victory: pays out, marks the scene
    /// cleared (or moves a replay on to its next scene) and resets the take. Then saves.
    /// Returns true on a scene's first clear. Does nothing when no run is active.
    /// </summary>
    public bool EndRun(RunOutcome outcome)
    {
        if (!IsActive) return false;
        if (_save.ActiveSlot != _slot)
        {
            // Never write one slot's run into another one.
            Debug.LogWarning($"[RunSession] The save slot changed during the run (slot {_slot} -> {_save.ActiveSlot}) - the run is dropped.");
            Finish();
            return false;
        }

        bool firstClear = false;
        SaveData data = _save.Data;
        int take = ProgressRules.TakeIndex(Act, Scene);

        data.tickets += _pendingTickets;
        if (outcome == RunOutcome.Victory)
        {
            data.bossTickets += _pendingBossTickets + BossTicketsPerVictory;
            data.takes[take] = 0;

            if (ProgressRules.IsFinished(data, Act))
            {
                data.replayScene[Act] = (Scene + 1) % ProgressRules.ScenesPerAct;
            }
            else
            {
                firstClear = Scene >= data.scenesCleared[Act];
                data.scenesCleared[Act] = Mathf.Max(data.scenesCleared[Act], Scene + 1);
            }
        }
        else
        {
            // Boss Tickets are never at risk on death (GDD > Economy).
            data.bossTickets += _pendingBossTickets;
            data.takes[take]++;
        }

        Finish();
        _save.Save();
        RunEnded?.Invoke(outcome, firstClear);
        return firstClear;
    }

    /// <summary>Quit to the menu mid-run: everything the run earned is dropped and nothing is saved.</summary>
    public void AbandonRun()
    {
        if (!IsActive) return;
        Finish();
    }

    private void Finish()
    {
        _pendingTickets = 0;
        _pendingBossTickets = 0;
        IsActive = false;
        _save.RunInProgress = false;
    }
}
