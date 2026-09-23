// ONE-OFF EDITOR TOOL (same pattern as EnemyMigrationTool): rebuilds ShopManager.powerUpsA
// through Unity's own pipeline - AssetDatabase resolution + SerializedObject assignment + a
// prefab save by Unity itself - instead of hand-written YAML references that currently
// deserialize as null at runtime.
// DELETE after the shop item list is confirmed working in play (RepoLayout one-off rule).
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ShopPowerUpRebuildTool
{
    private const string PrefabPath = "Assets/_Game/Rooms/shop_scene.prefab";
    private const string ItemsFolder = "Assets/_Game/Data/Items";

    [MenuItem("Tools/Shop/Rebuild Shop PowerUp List")]
    public static void Rebuild()
    {
        var items = new List<PowerUpData>();
        var failed = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:PowerUpData", new[] { ItemsFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<PowerUpData>(path);
            if (item == null)
            {
                failed.Add(path);
                Debug.LogError($"[ShopRebuild] AssetDatabase FAILED to load '{path}' as PowerUpData - the database entry for this asset is broken.");
                continue;
            }
            items.Add(item);
        }

        if (items.Count == 0)
        {
            Debug.LogError("[ShopRebuild] Zero items resolvable through the AssetDatabase - the database itself is broken. Delete Library and reopen Unity.");
            return;
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var shop = prefabRoot.GetComponentInChildren<ShopManager>(true);
            if (shop == null)
            {
                Debug.LogError("[ShopRebuild] No ShopManager found in the prefab.");
                return;
            }

            var so = new SerializedObject(shop);
            var list = so.FindProperty("powerUpsA");
            if (list == null)
            {
                Debug.LogError("[ShopRebuild] Serialized property 'powerUpsA' not found on ShopManager.");
                return;
            }

            list.ClearArray();
            for (int i = 0; i < items.Count; i++)
            {
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Debug.Log($"[ShopRebuild] powerUpsA rebuilt with {items.Count} items ({failed.Count} failed to resolve). Enter Play mode and check the [Shop] slot logs - they must now name real items.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}
