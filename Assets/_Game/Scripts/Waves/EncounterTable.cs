// TUNING DATA for one act's encounters and room rewards (GDD > Level Design > In-Run >
// "In-Run in Depth"). Each combat room buys enemies with a Threat Budget and spends it until
// nothing affordable is left; which enemy it buys is a weighted pick by scene, capped per room.
// Lives in Data/Rooms next to the RoomConfigLibrary; assigned on every room prefab's WaveManager.
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EncounterTable_Act1", menuName = "Rooms/Encounter Table")]
public class EncounterTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Designer name (Card Soldier, Flower...). Only for the inspector and logs.")]
        public string displayName;
        [Tooltip("Left empty = never spawned (e.g. an enemy whose prefab doesn't exist yet).")]
        public GameObject prefab;
        [Tooltip("Close / Ranged / Heavy / Spawner. Informational: tier comes from the scene, not the role.")]
        public EnemyType role;
        [Tooltip("Threat Budget points this enemy costs.")]
        [Min(1)] public int cost = 1;
        [Tooltip("Most of this enemy one room can hold.")]
        [Min(0)] public int maxPerRoom = 1;
        [Tooltip("Most of this enemy a First Spot room can hold. 0 = not allowed in the First Spot.")]
        [Min(0)] public int firstSpotMax;
        [Tooltip("First scene of the act (1-based) this enemy can appear in.")]
        [Min(1)] public int firstScene = 1;
        [Tooltip("Relative pick frequency per scene: element 0 = Scene 1. Past the last element the last one is used.")]
        public float[] weightPerScene = { 1f, 1f, 1f };

        public float WeightFor(int scene)
        {
            if (weightPerScene == null || weightPerScene.Length == 0) return 0f;
            int i = Mathf.Clamp(scene - 1, 0, weightPerScene.Length - 1);
            return Mathf.Max(0f, weightPerScene[i]);
        }

        public int CapFor(bool isFirstSpot) => isFirstSpot ? Mathf.Min(maxPerRoom, firstSpotMax) : maxPerRoom;
    }

    [Header("Encounters")]
    public List<Entry> enemies = new List<Entry>();

    [Header("Threat Budget (PLACEHOLDER - GDD numbers are still 'in process')")]
    [Tooltip("Room budget of a Fight / More Health / More Buttons room, per scene: element 0 = Scene 1.")]
    public int[] roomBudgetPerScene = { 6, 8, 10 };
    [Tooltip("Room budget of a First Spot room, per scene: element 0 = Scene 1.")]
    public int[] firstSpotBudgetPerScene = { 4, 5, 6 };
    [Tooltip("'Rooms get harder every two rooms': budget added for every two rooms already cleared in the node.")]
    public int budgetStepPerTwoRooms = 1;

    [Header("Rewards")]
    [Tooltip("Room clear pays ceil(room budget * this) Buttons. GDD: half the budget.")]
    public float buttonsPerBudgetPoint = 0.5f;
    [Tooltip("Room-clear Buttons multiplier in a More Buttons node.")]
    public float moreButtonsMultiplier = 1.5f;
    [Tooltip("Chance (0-1) of a +20 HP drop on room clear.")]
    [Range(0f, 1f)] public float healthDropChance = 0.15f;
    [Range(0f, 1f)] public float healthDropChanceMoreHealth = 0.45f;
    [Tooltip("Chance (0-1) that a regular enemy kill drops 1 Button.")]
    [Range(0f, 1f)] public float killButtonChance = 0.10f;
    [Range(0f, 1f)] public float killButtonChanceMoreButtons = 0.25f;
    [Tooltip("Seconds after the clear before the Buttons fly to the player.")]
    [Min(0f)] public float buttonFlyDelay = 1f;
    [Tooltip("Speed (m/s) the Buttons fly to the player at.")]
    [Min(0.1f)] public float buttonFlySpeed = 15f;

    // ---- Budget -------------------------------------------------------------------------

    public int RoomBudget(EncounterContext context)
    {
        int[] perScene = context.isFirstSpot ? firstSpotBudgetPerScene : roomBudgetPerScene;
        int baseBudget = perScene == null || perScene.Length == 0
            ? 0
            : perScene[Mathf.Clamp(context.scene - 1, 0, perScene.Length - 1)];
        return baseBudget + Mathf.Max(0, context.roomInNode) / 2 * budgetStepPerTwoRooms;
    }

    // ---- Rewards ------------------------------------------------------------------------

    // The Final Fight pays Boss Tickets only (RunSession, on the win): Buttons would be discarded
    // when the scene ends anyway.
    public int RoomClearButtons(int roomBudget, EncounterNodeKind nodeKind)
    {
        if (nodeKind == EncounterNodeKind.FinalFight) return 0;

        int buttons = Mathf.CeilToInt(roomBudget * buttonsPerBudgetPoint);
        if (nodeKind == EncounterNodeKind.MoreButtons) buttons = Mathf.CeilToInt(buttons * moreButtonsMultiplier);
        return Mathf.Max(0, buttons);
    }

    public float HealthDropChance(EncounterNodeKind nodeKind) => nodeKind switch
    {
        EncounterNodeKind.FinalFight => 0f,
        EncounterNodeKind.MoreHealth => healthDropChanceMoreHealth,
        _ => healthDropChance
    };

    public float KillButtonChance(EncounterNodeKind nodeKind) => nodeKind switch
    {
        EncounterNodeKind.FinalFight => 0f,
        EncounterNodeKind.MoreButtons => killButtonChanceMoreButtons,
        _ => killButtonChance
    };

    // ---- Picking ------------------------------------------------------------------------

    /// <summary>
    /// Buys enemies with <paramref name="budget"/> until nothing affordable is left: each buy is a
    /// weighted pick among the entries allowed in this scene, under their cap and within budget.
    /// <paramref name="counts"/> holds how many of each entry the room already has (index = entry
    /// index) and is updated, so caps hold across waves. <paramref name="random01"/> returns [0,1).
    /// </summary>
    public List<Entry> Plan(int budget, EncounterContext context, int[] counts, Func<float> random01)
    {
        var picks = new List<Entry>();
        var candidates = new List<int>();

        while (true)
        {
            candidates.Clear();
            float totalWeight = 0f;
            for (int i = 0; i < enemies.Count; i++)
            {
                Entry e = enemies[i];
                // A row added after the room started (live edit) has no count yet: skip it.
                if (i >= counts.Length || !IsAvailable(e, context, counts[i], budget)) continue;
                candidates.Add(i);
                totalWeight += e.WeightFor(context.scene);
            }
            if (candidates.Count == 0) break;

            float roll = random01() * totalWeight;
            int picked = candidates[candidates.Count - 1];
            foreach (int i in candidates)
            {
                roll -= enemies[i].WeightFor(context.scene);
                if (roll < 0f) { picked = i; break; }
            }

            counts[picked]++;
            // [Min(1)] only guards the inspector: a 0 cost set from code would buy up to the cap for free.
            budget -= Mathf.Max(1, enemies[picked].cost);
            picks.Add(enemies[picked]);
        }

        return picks;
    }

    public int IndexOf(Entry entry) => enemies.IndexOf(entry);

    private static bool IsAvailable(Entry e, EncounterContext context, int count, int budget) =>
        e != null
        && e.prefab != null
        && context.scene >= e.firstScene
        && e.WeightFor(context.scene) > 0f
        && count < e.CapFor(context.isFirstSpot)
        && e.cost <= budget;
}
