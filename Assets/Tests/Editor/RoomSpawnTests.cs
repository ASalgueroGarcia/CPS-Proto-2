using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Exercises the real spawn path - tier to budget to bodies on the ground - rather than
/// just the data behind it. The bug this guards against shipped for months: the tier was
/// computed on the map and never reached WaveManager, so every room spawned the Medium
/// default. Asserting asset values alone would not have caught that; only running a room does.
///
/// These are EditMode tests, so nothing calls Start(): they drive InitializeRoom() directly.
/// (Written before the game code had its own assemblies; a PlayMode test assembly could now
/// reference Spectracle.Waves and let Start() run.) Everything downstream of that call - the
/// budget loop, the ground raycast, the NavMesh sample, the Instantiate - is the shipping code.
///
/// The room is built from scratch rather than loaded from a Layout prefab: the prefabs carry
/// no NavMeshSurface, their NavMesh is baked per scene. A flat plane baked at runtime gives
/// the same placement path without depending on a scene's baked data.
/// </summary>
public class RoomSpawnTests
{
    private readonly List<GameObject> _created = new List<GameObject>();
    private NavMeshSurface _surface;

    [TearDown]
    public void TearDown()
    {
        if (_surface != null)
        {
            _surface.RemoveData();
            _surface = null;
        }
        foreach (GameObject go in _created.Where(go => go != null))
        {
            Object.DestroyImmediate(go);
        }
        _created.Clear();
    }

    private static void SetPrivateField(object target, string field, object value)
    {
        FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(info, $"WaveManager has no private field '{field}' - this test needs updating.");
        info.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string method)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(info, $"WaveManager has no private method '{method}' - this test needs updating.");
        info.Invoke(target, null);
    }

    private static RoomConfigLibrary LoadLibrary()
    {
        string[] guids = AssetDatabase.FindAssets("t:RoomConfigLibrary");
        Assert.AreEqual(1, guids.Length, "Expected exactly one RoomConfigLibrary asset.");
        return AssetDatabase.LoadAssetAtPath<RoomConfigLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    /// <summary>Flat walkable ground with a baked NavMesh, so spawn placement can actually succeed.</summary>
    private void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "TestGround";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(60f, 1f, 60f);
        _created.Add(ground);

        // Colliders are placed by the editor lazily; the spawner raycasts immediately.
        Physics.SyncTransforms();

        _surface = ground.AddComponent<NavMeshSurface>();
        _surface.collectObjects = CollectObjects.All;
        _surface.BuildNavMesh();

        Assert.IsTrue(NavMesh.SamplePosition(Vector3.zero, out _, 2f, NavMesh.AllAreas),
            "NavMesh bake produced nothing walkable - the spawn path cannot be exercised.");
    }

    /// <summary>A stand-in enemy: WaveManager only instantiates the prefab and subscribes to its Health.</summary>
    private GameObject BuildStubEnemyPrefab()
    {
        var stub = new GameObject("StubEnemy");
        stub.AddComponent<Health>();
        stub.SetActive(false); // a prefab source, never ticks itself
        _created.Add(stub);
        return stub;
    }

    /// <summary>A WaveManager wired like a real room, given its tier the way SceneController does.</summary>
    private WaveManager BuildRoom(RoomType tier, GameObject stubEnemy, System.Action<WaveManager> beforeStart = null)
    {
        var room = new GameObject($"TestRoom_{tier}");
        _created.Add(room);

        WaveManager waveManager = room.AddComponent<WaveManager>();

        // Every stub costs 1, so a correctly-driven room spends its whole budget on bodies.
        var configs = new List<WaveManager.EnemyPrefabConfig>
        {
            new WaveManager.EnemyPrefabConfig { type = EnemyType.Slimo,  prefab = stubEnemy, cost = 1 },
            new WaveManager.EnemyPrefabConfig { type = EnemyType.Ranged, prefab = stubEnemy, cost = 1 },
            new WaveManager.EnemyPrefabConfig { type = EnemyType.Heavy,  prefab = stubEnemy, cost = 1 },
        };
        SetPrivateField(waveManager, "enemyConfigs", configs);
        SetPrivateField(waveManager, "roomConfigLibrary", LoadLibrary());

        // The hand-off SceneController performs on the line after Instantiate.
        waveManager.SetRoomType(tier);
        beforeStart?.Invoke(waveManager);

        // Stands in for Start(); everything it calls is the shipping path.
        InvokePrivate(waveManager, "InitializeRoom");
        return waveManager;
    }

    private static int BodiesIn(WaveManager room) =>
        room.GetComponentsInChildren<Health>(true).Length;

    [Test]
    public void Room_ResolvesTheTierItWasGiven(
        [Values(RoomType.Entrance, RoomType.Medium, RoomType.Hard, RoomType.MiniBoss)] RoomType tier)
    {
        BuildGround();
        WaveManager room = BuildRoom(tier, BuildStubEnemyPrefab());

        Assert.IsNotNull(room.config, $"{tier}: WaveManager resolved no RoomConfigData.");
        Assert.AreEqual(tier, room.config.roomType,
            $"Room was told '{tier}' but resolved '{room.config.roomType}' - the tier is not reaching WaveManager.");
    }

    // Budgets stated here rather than read back from room.config: asserting against the
    // config the room resolved would stay self-consistent even if the tier were ignored,
    // which is the one bug this file exists to catch.
    [TestCase(RoomType.Entrance, 4)]
    [TestCase(RoomType.Medium, 6)]
    [TestCase(RoomType.Hard, 12)]
    [TestCase(RoomType.MiniBoss, 16)]
    public void Room_SpawnsEnemiesWorthItsBudget(RoomType tier, int expectedBudget)
    {
        BuildGround();
        WaveManager room = BuildRoom(tier, BuildStubEnemyPrefab());

        Assert.AreEqual(expectedBudget, BodiesIn(room),
            $"A {tier} room should spend a budget of {expectedBudget} on enemies but spawned {BodiesIn(room)}.");
    }

    [Test]
    public void DifferentTiers_ProduceDifferentRoomSizes()
    {
        // The regression in one assertion: if the tier were ignored, both rooms would fall
        // back to the same inspector default and these counts would match.
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();

        int entrance = BodiesIn(BuildRoom(RoomType.Entrance, stub));
        int miniBoss = BodiesIn(BuildRoom(RoomType.MiniBoss, stub));

        Assert.Less(entrance, miniBoss,
            $"An Entrance room ({entrance}) should be smaller than a MiniBoss room ({miniBoss}). " +
            "Equal counts mean the tier is ignored and every room is the same difficulty.");
    }

    [Test]
    public void ShopRoom_SpawnsNoEnemies()
    {
        BuildGround();
        WaveManager shop = BuildRoom(RoomType.Shop, BuildStubEnemyPrefab());

        Assert.AreEqual(0, BodiesIn(shop),
            "A Shop room took the combat path - InitializeRoom should return early for Shop and Treasure.");
    }

    // ---- GDD encounter rules (EncounterTable + map context) ---------------------------------

    private EncounterTable SingleEntryTable(GameObject prefab, int roomBudget)
    {
        var table = ScriptableObject.CreateInstance<EncounterTable>();
        table.enemies = new List<EncounterTable.Entry>
        {
            new EncounterTable.Entry { displayName = "Stub", prefab = prefab, cost = 1, maxPerRoom = 100, firstSpotMax = 100, firstScene = 1, weightPerScene = new[] { 1f } }
        };
        table.roomBudgetPerScene = new[] { roomBudget };
        table.budgetStepPerTwoRooms = 1;
        return table;
    }

    [Test]
    public void Room_WithEncounterContext_SpendsTheTableBudget()
    {
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = SingleEntryTable(stub, 5);

        // Room 3 of its node (index 2): one step up from the scene's 5.
        WaveManager room = BuildRoom(RoomType.Entrance, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.Fight, false, 2));
        });
        Object.DestroyImmediate(table);

        Assert.AreEqual(6, BodiesIn(room), "Budget 5 + 1 step for rooms 3-4 of the node should buy 6 cost-1 enemies.");
    }

    [Test]
    public void DeckOfCards_KeepsTheRoomOpenUntilItsSoldiersDie()
    {
        BuildGround();
        GameObject deckPrefab = BuildStubEnemyPrefab();
        deckPrefab.AddComponent<StubEnemySpawner>().soldierPrefab = BuildStubEnemyPrefab();
        EncounterTable table = SingleEntryTable(deckPrefab, 1);

        WaveManager room = BuildRoom(RoomType.Entrance, deckPrefab, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.Fight, false, 0));
        });

        StubEnemySpawner deck = room.GetComponentInChildren<StubEnemySpawner>(true);
        Assert.IsNotNull(deck, "The room should have spawned the Deck.");
        GameObject soldier = deck.Spawn();
        _created.Add(soldier);

        deck.GetComponent<Health>().OnDeath.Invoke();
        Assert.IsFalse(RoomCleared(room), "The Deck died but its soldier lives - the room must stay open.");

        soldier.GetComponent<Health>().OnDeath.Invoke();
        Assert.IsTrue(RoomCleared(room), "Deck and every soldier it made are dead - the room should clear.");
        Object.DestroyImmediate(table);
    }

    // ---- Edge cases: no map context yet, empty table, zero budget, rewards ------------------

    /// <summary>A stub table with no reward randomness: no kill drops, no health drop.</summary>
    private EncounterTable QuietTable(GameObject prefab, int roomBudget)
    {
        EncounterTable table = SingleEntryTable(prefab, roomBudget);
        table.killButtonChance = 0f;
        table.killButtonChanceMoreButtons = 0f;
        table.healthDropChance = 0f;
        table.healthDropChanceMoreHealth = 0f;
        return table;
    }

    private GameObject BuildPickupPrefab(string name, bool coin)
    {
        var pickup = new GameObject(name);
        if (coin) pickup.AddComponent<Coin>();
        pickup.SetActive(false);
        _created.Add(pickup);
        return pickup;
    }

    private static T GetPrivateField<T>(WaveManager room, string field) =>
        (T)typeof(WaveManager).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);

    /// <summary>Kills every enemy in the room; returns the pickups the room spawned (also queued for teardown).</summary>
    private List<GameObject> KillAllAndCollectDrops(WaveManager room)
    {
        foreach (Health h in room.GetComponentsInChildren<Health>(true).ToArray()) h.OnDeath.Invoke();
        List<GameObject> pickups = GetPrivateField<List<GameObject>>(room, "spawnedPickups").ToList();
        _created.AddRange(pickups);
        return pickups;
    }

    private static int CoinsIn(List<GameObject> pickups) => pickups.Count(p => p != null && p.GetComponent<Coin>() != null);

    [Test]
    public void Room_WithoutContext_PlaysTheLegacyRoom()
    {
        // A room the map gives no context plays exactly as before the encounter rules.
        BuildGround();
        GameObject soldier = BuildStubEnemyPrefab();
        soldier.name = "SoldierStub";
        GameObject flower = BuildStubEnemyPrefab();
        flower.name = "FlowerStub";
        EncounterTable table = QuietTable(soldier, 99);
        table.enemies.Add(new EncounterTable.Entry
        {
            displayName = "Flower", prefab = flower, cost = 1, maxPerRoom = 100, firstSpotMax = 100,
            firstScene = 2, weightPerScene = new[] { 1000f, 1000f }
        });

        WaveManager room = BuildRoom(RoomType.Entrance, soldier, wm => SetPrivateField(wm, "encounterTable", table));

        Health[] bodies = room.GetComponentsInChildren<Health>(true);
        Assert.AreEqual(4, bodies.Length, "No context: an Entrance room spends its tier budget (4), not the table's 99.");
        Assert.IsFalse(bodies.Any(b => b.name.StartsWith("FlowerStub")), "No context: enemies come from the Enemy Configs, not the table.");
        Object.DestroyImmediate(table);
    }

    [Test]
    public void Room_WithoutContext_PaysTheTierRewards()
    {
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(stub, 99);

        WaveManager room = BuildRoom(RoomType.Entrance, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            SetPrivateField(wm, "coinPrefab", BuildPickupPrefab("CoinStub", true));
        });
        List<GameObject> drops = KillAllAndCollectDrops(room);

        // Entrance pays 1 coin; the GDD formula would have paid ceil(4 / 2) = 2.
        Assert.AreEqual(1, CoinsIn(drops), "Without a map context the room pays its tier's expectedCurrency.");
        Object.DestroyImmediate(table);
    }

    [TestCase(EncounterNodeKind.Fight, 4)]
    [TestCase(EncounterNodeKind.MoreHealth, 4)]
    [TestCase(EncounterNodeKind.MoreButtons, 6)]
    public void Room_WithContext_PaysTheRoomClearButtons(EncounterNodeKind kind, int expected)
    {
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(stub, 7);

        WaveManager room = BuildRoom(RoomType.Entrance, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            SetPrivateField(wm, "coinPrefab", BuildPickupPrefab("CoinStub", true));
            wm.SetEncounterContext(new EncounterContext(1, kind, false, 0));
        });
        Assert.AreEqual(7, BodiesIn(room));
        List<GameObject> drops = KillAllAndCollectDrops(room);

        Assert.AreEqual(expected, CoinsIn(drops), $"A 7-point {kind} room should pay {expected} Buttons.");
        Assert.IsTrue(RoomCleared(room));
        Object.DestroyImmediate(table);
    }

    [Test]
    public void Room_WithZeroBudget_ClearsAtOnceAndPaysNothing()
    {
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(stub, 0);
        table.budgetStepPerTwoRooms = 0;

        WaveManager room = BuildRoom(RoomType.Entrance, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            SetPrivateField(wm, "coinPrefab", BuildPickupPrefab("CoinStub", true));
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.Fight, false, 0));
        });
        _created.AddRange(GetPrivateField<List<GameObject>>(room, "spawnedPickups"));

        Assert.AreEqual(0, BodiesIn(room));
        Assert.IsTrue(RoomCleared(room), "A room with nothing to spawn must not stay locked.");
        Assert.AreEqual(0, CoinsIn(GetPrivateField<List<GameObject>>(room, "spawnedPickups")), "A 0-point room pays 0 Buttons, not the tier's.");
        Assert.IsTrue(GetPrivateField<bool>(room, "roomCompleted"), "No pickups: the room completes right away.");
        Object.DestroyImmediate(table);
    }

    [Test]
    public void Room_WithEmptyTable_ClearsWithoutSpawning()
    {
        BuildGround();
        var table = ScriptableObject.CreateInstance<EncounterTable>();
        table.enemies = new List<EncounterTable.Entry>();

        WaveManager room = BuildRoom(RoomType.Entrance, BuildStubEnemyPrefab(), wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.Fight, false, 0));
        });

        Assert.AreEqual(0, BodiesIn(room));
        Assert.IsTrue(RoomCleared(room), "An empty table spawns nothing; the room must still clear.");
        Object.DestroyImmediate(table);
    }

    [Test]
    public void Room_WithoutContext_HasNoKillDrops()
    {
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(stub, 99);
        table.killButtonChance = 1f;

        WaveManager room = BuildRoom(RoomType.Entrance, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            SetPrivateField(wm, "coinPrefab", BuildPickupPrefab("CoinStub", true));
        });
        List<GameObject> drops = KillAllAndCollectDrops(room);

        Assert.AreEqual(1, CoinsIn(drops), "No context: only the tier's room-clear coin, no GDD kill drops.");
        Object.DestroyImmediate(table);
    }

    [Test]
    public void FinalFight_KeepsTheTierSpawning_AndPaysNoButtonsOrHealth()
    {
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(stub, 99);
        table.killButtonChance = 1f;
        table.healthDropChance = 1f;

        WaveManager room = BuildRoom(RoomType.Entrance, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            SetPrivateField(wm, "coinPrefab", BuildPickupPrefab("CoinStub", true));
            SetPrivateField(wm, "healthDropPrefab", BuildPickupPrefab("HealthStub", false));
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.FinalFight, false, 0));
        });
        Assert.AreEqual(4, BodiesIn(room), "Until boss prefabs exist the Final Fight spawns its tier's budget.");
        List<GameObject> drops = KillAllAndCollectDrops(room);

        Assert.IsTrue(RoomCleared(room));
        Assert.AreEqual(0, drops.Count, "GDD: the Final Fight pays Boss Tickets only - no Buttons, kill drops or health.");
        Assert.IsTrue(GetPrivateField<bool>(room, "roomCompleted"), "Nothing on the floor: the room completes right away.");
        Object.DestroyImmediate(table);
    }

    [Test]
    public void DeckSoldiers_DropNoButtons_RegularKillsDo()
    {
        BuildGround();
        GameObject deckPrefab = BuildStubEnemyPrefab();
        deckPrefab.AddComponent<StubEnemySpawner>().soldierPrefab = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(deckPrefab, 1);
        table.killButtonChance = 1f;      // every regular kill drops
        table.buttonsPerBudgetPoint = 0f; // isolate kill drops from the room-clear pay

        WaveManager room = BuildRoom(RoomType.Entrance, deckPrefab, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            SetPrivateField(wm, "coinPrefab", BuildPickupPrefab("CoinStub", true));
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.Fight, false, 0));
        });
        StubEnemySpawner deck = room.GetComponentInChildren<StubEnemySpawner>(true);
        GameObject soldier = deck.Spawn();
        _created.Add(soldier);

        soldier.GetComponent<Health>().OnDeath.Invoke();
        Assert.AreEqual(0, CoinsIn(GetPrivateField<List<GameObject>>(room, "spawnedPickups")), "A Deck's soldier never drops Buttons.");

        deck.GetComponent<Health>().OnDeath.Invoke();
        List<GameObject> drops = GetPrivateField<List<GameObject>>(room, "spawnedPickups");
        _created.AddRange(drops);
        Assert.AreEqual(1, CoinsIn(drops), "The Deck itself is a regular kill: 1 Button at a 100% kill-drop rate.");
        Object.DestroyImmediate(table);
    }

    [Test]
    public void ExpensiveEnemies_FitInAMultiWaveRoom()
    {
        // A Hard room has 3 waves. Splitting an 8-point budget first (3/3/2) would shut the cost-4
        // Deck out of every wave; the room is bought whole and then dealt out.
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(stub, 8);
        table.enemies[0].weightPerScene = new[] { 0.001f };
        table.enemies.Add(new EncounterTable.Entry
        {
            displayName = "Deck", prefab = stub, cost = 4, maxPerRoom = 1, firstSpotMax = 0,
            firstScene = 1, weightPerScene = new[] { 1000f }
        });

        WaveManager room = BuildRoom(RoomType.Hard, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.Fight, false, 0));
        });

        var plan = GetPrivateField<List<EncounterTable.Entry>>(room, "roomPlan");
        Assert.AreEqual(1, plan.Count(p => p.displayName == "Deck"), "The Deck should be bought in an 8-point room.");
        Assert.AreEqual(8, plan.Sum(p => p.cost), "The room plan should spend the whole budget.");
        Assert.Less(BodiesIn(room), plan.Count, "Wave 1 takes only its share of the room.");
        Object.DestroyImmediate(table);
    }

    [Test]
    public void Room_EndsWhenThePlanRunsOut_NoEmptyWaves()
    {
        // A Medium room has 2 waves; a 1-point plan fits in wave 1, so wave 2 must not make the player wait.
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(stub, 1);
        table.budgetStepPerTwoRooms = 0;

        WaveManager room = BuildRoom(RoomType.Medium, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.Fight, false, 0));
        });
        Assert.AreEqual(1, BodiesIn(room));
        KillAllAndCollectDrops(room);

        Assert.IsTrue(RoomCleared(room), "Nothing left to spawn: the room clears on the last kill instead of scheduling an empty wave.");
        Object.DestroyImmediate(table);
    }

    [Test]
    public void HealthDrop_DoesNotHoldTheRoom()
    {
        BuildGround();
        GameObject stub = BuildStubEnemyPrefab();
        EncounterTable table = QuietTable(stub, 2);
        table.healthDropChance = 1f;
        table.buttonsPerBudgetPoint = 0f;

        WaveManager room = BuildRoom(RoomType.Entrance, stub, wm =>
        {
            SetPrivateField(wm, "encounterTable", table);
            SetPrivateField(wm, "healthDropPrefab", BuildPickupPrefab("HealthStub", false));
            wm.SetEncounterContext(new EncounterContext(1, EncounterNodeKind.Fight, false, 0));
        });
        List<GameObject> drops = KillAllAndCollectDrops(room);

        Assert.AreEqual(1, drops.Count, "The 100% health drop should be on the floor.");
        Assert.IsTrue(GetPrivateField<bool>(room, "roomCompleted"), "GDD: the health drop stays on the floor and doesn't block the end of the room.");
        Object.DestroyImmediate(table);
    }

    private static bool RoomCleared(WaveManager room) =>
        (bool)typeof(WaveManager).GetField("roomCleared", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(room);
}
