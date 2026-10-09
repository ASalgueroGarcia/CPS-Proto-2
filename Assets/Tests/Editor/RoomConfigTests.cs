using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Guards the room tuning data now that it lives in assets instead of the hardcoded
/// RoomConfigs table. These are the rules the GDD's Run Progression table describes:
/// if an asset drifts from it, or a room prefab loses its library reference, the game
/// silently spawns the wrong thing - so assert it here rather than in play mode.
/// </summary>
public class RoomConfigTests
{
    /// <summary>The Run Progression table from the GDD, mirrored here as the source of truth.</summary>
    private static readonly Dictionary<RoomType, Expected> DesignTable = new Dictionary<RoomType, Expected>
    {
        { RoomType.Entrance,   new Expected(4,  1, 1, 0f,   EnemyType.Slimo) },
        { RoomType.Medium,     new Expected(6,  2, 2, 0.1f, EnemyType.Slimo, EnemyType.Ranged) },
        { RoomType.MediumHard, new Expected(9,  2, 2, 0.2f, EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy) },
        { RoomType.Hard,       new Expected(12, 3, 3, 0.3f, EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy) },
        { RoomType.MiniBoss,   new Expected(16, 3, 4, 0.6f, EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy) },
        { RoomType.Boss,       new Expected(12, 3, 6, 1f,   EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy) },
        { RoomType.Shop,       new Expected(0,  0, 0, 0f) },
        { RoomType.Treasure,   new Expected(0,  0, 0, 1f) },
    };

    private struct Expected
    {
        public readonly int Budget;
        public readonly int MaxWaves;
        public readonly int Currency;
        public readonly float HealthDropChance;
        public readonly EnemyType[] Pool;

        public Expected(int budget, int maxWaves, int currency, float healthDropChance, params EnemyType[] pool)
        {
            Budget = budget;
            MaxWaves = maxWaves;
            Currency = currency;
            HealthDropChance = healthDropChance;
            Pool = pool;
        }
    }

    private static IEnumerable<RoomType> AllRoomTypes => DesignTable.Keys;

    private static RoomConfigLibrary LoadLibrary()
    {
        string[] guids = AssetDatabase.FindAssets("t:RoomConfigLibrary");
        Assert.AreEqual(1, guids.Length,
            "Expected exactly one RoomConfigLibrary asset in the project; a second one would let " +
            "different room prefabs disagree about the tuning.");

        var library = AssetDatabase.LoadAssetAtPath<RoomConfigLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
        Assert.IsNotNull(library, "RoomConfigLibrary asset failed to load.");
        return library;
    }

    [Test]
    public void Library_ResolvesEveryRoomType()
    {
        RoomConfigLibrary library = LoadLibrary();

        foreach (RoomType tier in AllRoomTypes)
        {
            RoomConfigData config = library.Get(tier);
            Assert.IsNotNull(config, $"No RoomConfigData for tier '{tier}'. WaveManager would spawn nothing in that room.");
            Assert.AreEqual(tier, config.roomType, $"Asset '{config.name}' is filed under tier '{tier}' but declares '{config.roomType}'.");
        }
    }

    [Test]
    public void Library_CoversEveryRoomTypeInTheEnum()
    {
        // Catches a tier added to the enum with no asset created for it.
        RoomConfigLibrary library = LoadLibrary();
        var covered = library.configs.Where(c => c != null).Select(c => c.roomType).ToList();

        foreach (RoomType tier in System.Enum.GetValues(typeof(RoomType)))
        {
            Assert.Contains(tier, covered, $"RoomType.{tier} exists in the enum but has no RoomConfigData in the library.");
        }
    }

    [Test]
    public void Library_HasNoNullOrDuplicateEntries()
    {
        RoomConfigLibrary library = LoadLibrary();

        Assert.IsFalse(library.configs.Any(c => c == null),
            "The library has an empty slot - Get() would skip it and fall through to the error path.");

        var duplicates = library.configs.GroupBy(c => c.roomType).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.IsEmpty(duplicates, $"More than one config claims the same tier: {string.Join(", ", duplicates)}. Get() returns whichever comes first.");
    }

    [Test]
    public void AssetValues_MatchTheDesignTable([ValueSource(nameof(AllRoomTypes))] RoomType tier)
    {
        RoomConfigData config = LoadLibrary().Get(tier);
        Expected expected = DesignTable[tier];

        Assert.AreEqual(expected.Budget, config.budget, $"{tier}: budget drifted from the GDD table.");
        Assert.AreEqual(expected.MaxWaves, config.maxWaves, $"{tier}: maxWaves drifted from the GDD table.");
        Assert.AreEqual(expected.Currency, config.expectedCurrency, $"{tier}: expectedCurrency drifted from the GDD table.");
        Assert.AreEqual(expected.HealthDropChance, config.healthDropChance, 0.0001f, $"{tier}: healthDropChance drifted from the GDD table.");
        CollectionAssert.AreEquivalent(expected.Pool, config.enemyPool, $"{tier}: enemy pool drifted from the GDD table.");
    }

    [Test]
    public void EveryRoomPrefab_IsWiredToTheLibrary()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Rooms" });
        Assert.IsNotEmpty(guids, "No room prefabs found under Assets/_Game/Rooms.");

        var unwired = new List<string>();
        int checkedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            foreach (WaveManager waveManager in prefab.GetComponentsInChildren<WaveManager>(true))
            {
                checkedCount++;

                // roomConfigLibrary is private and serialized, so read it the way Unity stores it.
                var serialized = new SerializedObject(waveManager);
                SerializedProperty property = serialized.FindProperty("roomConfigLibrary");
                Assert.IsNotNull(property, $"{path}: WaveManager has no serialized 'roomConfigLibrary' field.");

                if (property.objectReferenceValue == null) unwired.Add(path);
            }
        }

        Assert.Greater(checkedCount, 0, "Found room prefabs but none carried a WaveManager.");
        Assert.IsEmpty(unwired,
            "These room prefabs have no RoomConfigLibrary assigned, so they log an error and spawn " +
            $"no enemies at runtime:\n  {string.Join("\n  ", unwired)}");
    }
}
