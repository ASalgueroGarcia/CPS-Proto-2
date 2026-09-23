// ONE-OFF migration tool (EnemyMigrationTool pattern): exports WaveManager's hardcoded
// RoomConfigs static table into Data/Rooms ScriptableObjects, creates a RoomConfigLibrary
// asset, and wires the library into every room prefab's WaveManager.
// DELETE after it runs successfully (RepoLayout one-off rule).
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RoomConfigMigrationTool
{
    private const string DataFolder = "Assets/_Game/Data";
    private const string RoomsFolder = DataFolder + "/Rooms";
    private const string LibraryPath = RoomsFolder + "/RoomConfigLibrary.asset";
    private const string RoomsPrefabsFolder = "Assets/_Game/Rooms";

    private static RoomConfigLibrary _library;

    [MenuItem("Tools/Waves/Migrate RoomConfig Table to Assets")]
    public static void Migrate()
    {
        if (!AssetDatabase.IsValidFolder(RoomsFolder))
            AssetDatabase.CreateFolder(DataFolder, "Rooms");

        _library = AssetDatabase.LoadAssetAtPath<RoomConfigLibrary>(LibraryPath);
        if (_library == null)
        {
            _library = ScriptableObject.CreateInstance<RoomConfigLibrary>();
            AssetDatabase.CreateAsset(_library, LibraryPath);
        }
        _library.configs.Clear();

        MigrateTier(RoomType.Entrance, 4, 1, new List<EnemyType> { EnemyType.Slimo }, 1, 0f);
        MigrateTier(RoomType.Medium, 6, 2, new List<EnemyType> { EnemyType.Slimo, EnemyType.Ranged }, 2, 0.1f);
        MigrateTier(RoomType.MediumHard, 9, 2, new List<EnemyType> { EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy }, 2, 0.2f);
        MigrateTier(RoomType.Hard, 12, 3, new List<EnemyType> { EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy }, 3, 0.3f);
        MigrateTier(RoomType.MiniBoss, 16, 3, new List<EnemyType> { EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy }, 4, 0.6f);
        MigrateTier(RoomType.Boss, 12, 3, new List<EnemyType> { EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy }, 6, 1.0f);
        MigrateTier(RoomType.Shop, 0, 0, new List<EnemyType>(), 0, 0f);
        MigrateTier(RoomType.Treasure, 0, 0, new List<EnemyType>(), 0, 1.0f);

        EditorUtility.SetDirty(_library);
        AssetDatabase.SaveAssets();

        int wired = 0;
        foreach (var prefabGuid in AssetDatabase.FindAssets("t:Prefab", new[] { RoomsPrefabsFolder }))
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var waveManager = root.GetComponentInChildren<WaveManager>(true);
                if (waveManager == null) continue;

                var so = new SerializedObject(waveManager);
                var prop = so.FindProperty("roomConfigLibrary");
                if (prop == null)
                {
                    Debug.LogError($"[RoomMigrate] WaveManager in {prefabPath} has no roomConfigLibrary property - recompile first, then run again.");
                    continue;
                }
                prop.objectReferenceValue = _library;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                wired++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        Debug.Log($"[RoomMigrate] Done: {(_library != null ? _library.configs.Count : 0)} tier assets created in Data/Rooms, {wired} room prefabs wired to RoomConfigLibrary.asset.");
    }

    private static void MigrateTier(RoomType type, int budget, int maxWaves, List<EnemyType> pool, int currency, float healthDropChance)
    {
        var path = $"{RoomsFolder}/RoomConfig_{type}.asset";
        var data = AssetDatabase.LoadAssetAtPath<RoomConfigData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<RoomConfigData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.roomType = type;
        data.budget = budget;
        data.maxWaves = maxWaves;
        data.enemyPool = pool;
        data.expectedCurrency = currency;
        data.healthDropChance = healthDropChance;
        EditorUtility.SetDirty(data);

        _library.configs.Add(data);
    }
}
