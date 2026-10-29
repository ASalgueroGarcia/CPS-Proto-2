using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// The GDD encounter rules (In-Run in Depth: Encounters + Rewards) on the EncounterTable:
/// weighted picks, per-room and First Spot caps, scene gating, budget spending and the
/// reward formulas. Pure data - no room, NavMesh or Start() involved.
/// </summary>
public class EncounterTableTests
{
    private readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in _created.Where(o => o != null)) Object.DestroyImmediate(o);
        _created.Clear();
    }

    private GameObject Prefab(string name)
    {
        var go = new GameObject(name);
        _created.Add(go);
        return go;
    }

    private static EncounterTable.Entry Entry(string name, GameObject prefab, int cost, int max, int firstSpotMax, int firstScene, params float[] weights) =>
        new EncounterTable.Entry
        {
            displayName = name, prefab = prefab, cost = cost, maxPerRoom = max,
            firstSpotMax = firstSpotMax, firstScene = firstScene, weightPerScene = weights
        };

    /// <summary>The Act 1 table as the GDD writes it (Caterpillar 15/13/15).</summary>
    private EncounterTable Act1Table()
    {
        var table = ScriptableObject.CreateInstance<EncounterTable>();
        _created.Add(table);
        table.enemies = new List<EncounterTable.Entry>
        {
            Entry("Card Soldier",  Prefab("Soldier"),     1, 8, 8, 1, 50, 35, 30),
            Entry("Flower",        Prefab("Flower"),      1, 4, 4, 2,  0, 20, 20),
            Entry("White Rabbit",  Prefab("Rabbit"),      2, 3, 1, 1, 30, 25, 25),
            Entry("Caterpillar",   Prefab("Caterpillar"), 3, 2, 0, 1, 15, 13, 15),
            Entry("Deck of Cards", Prefab("Deck"),        4, 1, 0, 2,  5,  7, 10),
        };
        return table;
    }

    private static Func<float> Seeded(int seed)
    {
        var rng = new System.Random(seed);
        return () => (float)rng.NextDouble();
    }

    private static Dictionary<string, int> Count(IEnumerable<EncounterTable.Entry> picks) =>
        picks.GroupBy(p => p.displayName).ToDictionary(g => g.Key, g => g.Count());

    private static int Get(Dictionary<string, int> counts, string name) =>
        counts.TryGetValue(name, out int n) ? n : 0;

    [Test]
    public void WeightedPick_FollowsTheWeights()
    {
        // One cost-1 slot per draw and no caps in reach, so every draw is a fresh weighted pick.
        EncounterTable table = Act1Table();
        table.enemies = table.enemies.Take(1).ToList();
        table.enemies.Add(Entry("Other", Prefab("Other"), 1, 10000, 10000, 1, 150, 1, 1));
        table.enemies[0].maxPerRoom = 10000;

        var counts = new int[2];
        Func<float> rng = Seeded(1);
        for (int i = 0; i < 4000; i++)
            table.Plan(1, new EncounterContext(1, EncounterNodeKind.Fight, false, 0), counts, rng);

        // Weights 50 : 150 -> 25% soldiers.
        float soldierShare = counts[0] / 4000f;
        Assert.AreEqual(0.25f, soldierShare, 0.03f, $"Soldier share {soldierShare:P1}, expected ~25% (weight 50 of 200).");
    }

    [Test]
    public void Plan_SpendsTheBudgetUntilNothingIsAffordable([Values(1, 2, 3, 4, 5)] int seed)
    {
        EncounterTable table = Act1Table();
        var picks = table.Plan(7, new EncounterContext(2, EncounterNodeKind.Fight, false, 0), new int[5], Seeded(seed));

        // Card Soldiers cost 1 and cap at 8, so a 7-point room always spends every point.
        Assert.AreEqual(7, picks.Sum(p => p.cost), "A 7-point room should spend its whole budget.");
    }

    [Test]
    public void Caps_HoldPerRoom_AcrossWaves([Values(1, 2, 3, 4, 5, 6, 7, 8)] int seed)
    {
        EncounterTable table = Act1Table();
        var context = new EncounterContext(3, EncounterNodeKind.Fight, false, 0);
        var counts = new int[5];
        Func<float> rng = Seeded(seed);

        // Three waves sharing one room's counts.
        var all = new List<EncounterTable.Entry>();
        for (int wave = 0; wave < 3; wave++) all.AddRange(table.Plan(20, context, counts, rng));

        var n = Count(all);
        Assert.LessOrEqual(Get(n, "Card Soldier"), 8);
        Assert.LessOrEqual(Get(n, "Flower"), 4);
        Assert.LessOrEqual(Get(n, "White Rabbit"), 3);
        Assert.LessOrEqual(Get(n, "Caterpillar"), 2);
        Assert.LessOrEqual(Get(n, "Deck of Cards"), 1);
    }

    [Test]
    public void FirstSpot_UsesTheSimplifiedPool([Values(1, 2, 3, 4, 5, 6, 7, 8)] int seed)
    {
        EncounterTable table = Act1Table();
        var n = Count(table.Plan(30, new EncounterContext(2, EncounterNodeKind.Fight, true, 0), new int[5], Seeded(seed)));

        Assert.AreEqual(0, Get(n, "Caterpillar"), "Caterpillars are not allowed in the First Spot.");
        Assert.AreEqual(0, Get(n, "Deck of Cards"), "Decks of Cards are not allowed in the First Spot.");
        Assert.LessOrEqual(Get(n, "White Rabbit"), 1, "The First Spot holds at most one White Rabbit.");
    }

    [Test]
    public void Scene1_HasNoFlowersOrDecks([Values(1, 2, 3, 4, 5, 6, 7, 8)] int seed)
    {
        EncounterTable table = Act1Table();
        var n = Count(table.Plan(30, new EncounterContext(1, EncounterNodeKind.Fight, false, 0), new int[5], Seeded(seed)));

        Assert.AreEqual(0, Get(n, "Flower"), "Flowers appear from Scene 2 (after the Bouquet).");
        Assert.AreEqual(0, Get(n, "Deck of Cards"), "Decks of Cards appear from Scene 2 (after the Bouquet).");
    }

    [Test]
    public void Gating_HoldsEvenWithWeight()
    {
        // firstScene gates on its own, not only through a 0 weight.
        EncounterTable table = Act1Table();
        table.enemies[4].weightPerScene = new float[] { 1000, 1000, 1000 };

        var n = Count(table.Plan(30, new EncounterContext(1, EncounterNodeKind.Fight, false, 0), new int[5], Seeded(3)));
        Assert.AreEqual(0, Get(n, "Deck of Cards"));
    }

    [Test]
    public void EntryWithoutPrefab_IsNeverPicked()
    {
        EncounterTable table = Act1Table();
        table.enemies[0].prefab = null;

        var n = Count(table.Plan(30, new EncounterContext(1, EncounterNodeKind.Fight, false, 0), new int[5], Seeded(5)));
        Assert.AreEqual(0, Get(n, "Card Soldier"));
    }

    [TestCase(7, EncounterNodeKind.Fight, 4)]
    [TestCase(8, EncounterNodeKind.Fight, 4)]
    [TestCase(7, EncounterNodeKind.MoreHealth, 4)]
    [TestCase(7, EncounterNodeKind.MoreButtons, 6)]
    [TestCase(8, EncounterNodeKind.MoreButtons, 6)]
    public void RoomClearButtons_FollowTheGdd(int budget, EncounterNodeKind kind, int expected)
    {
        Assert.AreEqual(expected, Act1Table().RoomClearButtons(budget, kind));
    }

    [Test]
    public void DropChances_FollowTheGdd()
    {
        EncounterTable table = Act1Table();
        Assert.AreEqual(0.15f, table.HealthDropChance(EncounterNodeKind.Fight), 1e-5f);
        Assert.AreEqual(0.45f, table.HealthDropChance(EncounterNodeKind.MoreHealth), 1e-5f);
        Assert.AreEqual(0.10f, table.KillButtonChance(EncounterNodeKind.Fight), 1e-5f);
        Assert.AreEqual(0.25f, table.KillButtonChance(EncounterNodeKind.MoreButtons), 1e-5f);
    }

    // GDD Rewards: "Final Fight - Boss Tickets". Buttons are discarded when the scene ends.
    [TestCase(7)]
    [TestCase(36)]
    public void FinalFight_PaysNoButtonsAndNoHealth(int budget)
    {
        EncounterTable table = Act1Table();
        Assert.AreEqual(0, table.RoomClearButtons(budget, EncounterNodeKind.FinalFight));
        Assert.AreEqual(0f, table.HealthDropChance(EncounterNodeKind.FinalFight));
        Assert.AreEqual(0f, table.KillButtonChance(EncounterNodeKind.FinalFight));
    }

    [Test]
    public void RoomBudget_StepsUpEveryTwoRooms()
    {
        EncounterTable table = Act1Table();
        table.roomBudgetPerScene = new[] { 6, 8, 10 };
        table.budgetStepPerTwoRooms = 1;

        int[] byRoom = Enumerable.Range(0, 5)
            .Select(r => table.RoomBudget(new EncounterContext(2, EncounterNodeKind.Fight, false, r)))
            .ToArray();
        CollectionAssert.AreEqual(new[] { 8, 8, 9, 9, 10 }, byRoom);
    }

    // ---- Edge cases -----------------------------------------------------------------------

    [Test]
    public void Plan_EmptyTable_BuysNothing()
    {
        var table = ScriptableObject.CreateInstance<EncounterTable>();
        _created.Add(table);
        table.enemies = new List<EncounterTable.Entry>();

        Assert.IsEmpty(table.Plan(10, new EncounterContext(1, EncounterNodeKind.Fight, false, 0), new int[0], Seeded(1)));
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void Plan_NoBudget_BuysNothing(int budget)
    {
        Assert.IsEmpty(Act1Table().Plan(budget, new EncounterContext(3, EncounterNodeKind.Fight, false, 0), new int[5], Seeded(1)));
    }

    [Test]
    public void Plan_SkipsNullRowsAndRowsWithoutWeights()
    {
        EncounterTable table = Act1Table();
        table.enemies[1] = null;
        table.enemies[2].weightPerScene = new float[0];
        table.enemies[3].weightPerScene = null;

        var n = Count(table.Plan(30, new EncounterContext(3, EncounterNodeKind.Fight, false, 0), new int[5], Seeded(2)));
        Assert.AreEqual(0, Get(n, "White Rabbit"));
        Assert.AreEqual(0, Get(n, "Caterpillar"));
        Assert.Greater(Get(n, "Card Soldier"), 0);
    }

    [Test]
    public void Scenes_PastTheTable_UseTheLastColumn()
    {
        EncounterTable table = Act1Table();
        Assert.AreEqual(15f, table.enemies[3].WeightFor(9));
        Assert.AreEqual(50f, table.enemies[0].WeightFor(0), "Scene 0 or less clamps to Scene 1.");
        Assert.AreEqual(10, table.RoomBudget(new EncounterContext(9, EncounterNodeKind.Fight, false, 0)));
    }

    [Test]
    public void RoomBudget_EmptyBudgetArrays_AreZero_AndNegativeRoomIndexAddsNothing()
    {
        EncounterTable table = Act1Table();
        table.roomBudgetPerScene = new int[0];
        Assert.AreEqual(0, table.RoomBudget(new EncounterContext(1, EncounterNodeKind.Fight, false, 0)));

        table.roomBudgetPerScene = new[] { 6 };
        Assert.AreEqual(6, table.RoomBudget(new EncounterContext(1, EncounterNodeKind.Fight, false, -4)));
    }

    [TestCase(0, EncounterNodeKind.Fight, 0)]
    [TestCase(0, EncounterNodeKind.MoreButtons, 0)]
    [TestCase(1, EncounterNodeKind.MoreButtons, 2)]
    public void RoomClearButtons_SmallBudgets(int budget, EncounterNodeKind kind, int expected)
    {
        Assert.AreEqual(expected, Act1Table().RoomClearButtons(budget, kind));
    }

    [Test]
    public void Act1Asset_MatchesTheGddTable()
    {
        string[] guids = AssetDatabase.FindAssets("t:EncounterTable");
        Assert.AreEqual(1, guids.Length, "Expected exactly one EncounterTable asset (Data/Rooms/EncounterTable_Act1).");
        var table = AssetDatabase.LoadAssetAtPath<EncounterTable>(AssetDatabase.GUIDToAssetPath(guids[0]));

        string Row(EncounterTable.Entry e) =>
            $"{e.displayName} cost {e.cost} max {e.maxPerRoom} first {e.firstSpotMax} from {e.firstScene} " +
            string.Join("/", e.weightPerScene.Select(w => w.ToString("0")));

        CollectionAssert.AreEqual(new[]
        {
            "Card Soldier cost 1 max 8 first 8 from 1 50/35/30",
            "Flower cost 1 max 4 first 4 from 2 0/20/20",
            "White Rabbit cost 2 max 3 first 1 from 1 30/25/25",
            "Caterpillar cost 3 max 2 first 0 from 1 15/13/15",
            "Deck of Cards cost 4 max 1 first 0 from 2 5/7/10",
        }, table.enemies.Select(Row).ToArray());

        // Every enemy that exists in the game has its prefab; the Deck of Cards has none yet.
        CollectionAssert.AreEqual(new[] { true, true, true, true, false }, table.enemies.Select(e => e.prefab != null).ToArray(),
            "Soldier, Flower, Rabbit and Caterpillar need prefabs; the Deck stays empty until it exists.");
    }

    [Test]
    public void EveryRoomPrefab_IsWiredToTheEncounterTable()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Rooms" });
        foreach (string guid in guids)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (WaveManager wm in prefab.GetComponentsInChildren<WaveManager>(true))
            {
                var so = new SerializedObject(wm);
                Assert.IsNotNull(so.FindProperty("encounterTable").objectReferenceValue,
                    $"{prefab.name}: WaveManager has no EncounterTable - it falls back to legacy uniform spawning.");
            }
        }
    }
}
