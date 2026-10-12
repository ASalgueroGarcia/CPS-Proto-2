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
/// These drive InitializeRoom() directly instead of waiting on Start(), because PlayMode
/// tests need a test assembly and an asmdef-based assembly cannot reference the predefined
/// Assembly-CSharp the game code lives in. Everything downstream of that call - the budget
/// loop, the ground raycast, the NavMesh sample, the Instantiate - is the shipping code.
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
    private WaveManager BuildRoom(RoomType tier, GameObject stubEnemy)
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
}
