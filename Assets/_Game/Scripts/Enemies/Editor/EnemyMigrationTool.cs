// LEGACY MIGRATION TOOL (review): Converts old controllers to the current system; not used during gameplay.
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.IO;
public static class EnemyMigrationTool
{
    private const string PrefabDir = "Assets/_Game/Prefabs/Enemies";

    [MenuItem("Tools/Enemy System/Migrate Prefabs to Component-Strategy Pattern")]
    public static void MigrateAll()
    {
        if (!EditorUtility.DisplayDialog(
            "Enemy System Migration",
            "This will:\n" +
            "• Store settings on each prefab Enemy component\n" +
            "• Rewire Enemy_Heavy and Enemy_Ranged prefabs\n" +
            "• Remove old HeavyEnemy/RangedEnemy components\n\n" +
            "Prefabs are saved automatically. Continue?",
            "Migrate", "Cancel"))
            return;

        var heavy = new EnemySettings { maxHealth = 80f, damage = 30f, speed = 2f, alertRange = 10f, attackRange = 2.5f, alertDuration = 1f, roamRadius = 8f, idleDuration = 2f, patrolSpeedMultiplier = 0.5f, windupDuration = 0.5f, attackCooldown = 1.5f, chaseLeashMultiplier = 1.5f };
        var ranged = new EnemySettings { maxHealth = 25f, damage = 30f, speed = 5f, alertRange = 30f, attackRange = 17f, alertDuration = 1f, roamRadius = 12f, idleDuration = 1.5f, patrolSpeedMultiplier = 0.5f, windupDuration = 0.5f, attackCooldown = 2f, chaseLeashMultiplier = 2f };
        MigratePrefab<HeavyEnemy, MeleeAttack>("Enemy_Heavy", heavy);
        MigratePrefab<RangedEnemy, RangedAttack>("Enemy_Ranged", ranged);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Migration Complete",
            "Heavy and Ranged prefabs have been processed for migration.\n\n" +
            "Next steps:\n" +
            "1. Open each prefab and verify the Enemy settings + strategy are wired correctly.\n" +
            "2. Delete EnemyBase.cs, BasicEnemy.cs, HeavyEnemy.cs, RangedEnemy.cs when ready.\n" +
            "3. Bake NavMesh (the prefabs now use NavMeshAgent instead of CharacterController).",
            "OK");
    }
    private static void MigratePrefab<TOld, TStrategy>(string prefabName, EnemySettings defaults)
        where TOld      : MonoBehaviour
        where TStrategy : MonoBehaviour, IEnemyAttackStrategy
    {
        string prefabPath = $"{PrefabDir}/{prefabName}.prefab";
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefabAsset == null)
        {
            Debug.LogWarning($"[EnemyMigration] Prefab not found at '{prefabPath}'. Skipping.");
            return;
        }
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            var oldScript = root.GetComponent<TOld>();
            if (oldScript != null)
            {
                Object.DestroyImmediate(oldScript, true);
                Debug.Log($"[EnemyMigration] Removed {typeof(TOld).Name} from '{prefabName}'.");
            }
            var characterController = root.GetComponent<CharacterController>();
            if (characterController != null)
            {
                Object.DestroyImmediate(characterController, true);
                Debug.Log($"[EnemyMigration] Removed CharacterController from '{prefabName}'.");
            }
            if (root.GetComponent<NavMeshAgent>() == null)
            {
                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius          = 0.5f;
                agent.height          = 2f;
                agent.speed           = defaults.speed;
                agent.angularSpeed    = 180f;
                agent.acceleration    = 8f;
                agent.stoppingDistance = 0f;
                agent.autoBraking     = true;
                Debug.Log($"[EnemyMigration] Added NavMeshAgent to '{prefabName}'.");
            }
            if (root.GetComponent<Health>() == null)
                root.AddComponent<Health>();
            TStrategy strategy = root.GetComponent<TStrategy>();
            if (strategy == null)
            {
                strategy = root.AddComponent<TStrategy>();
                Debug.Log($"[EnemyMigration] Added {typeof(TStrategy).Name} to '{prefabName}'.");
            }
            if (strategy is RangedAttack ranged)
            {
                if (ranged.idealDistance == 0f)
                    ranged.idealDistance = 15f;
            }
            Enemy enemy = root.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = root.AddComponent<Enemy>();
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(defaults), enemy.Settings);
                Debug.Log($"[EnemyMigration] Added Enemy orchestrator to '{prefabName}'.");
            }
            SerializedObject serializedEnemy = new SerializedObject(enemy);
            serializedEnemy.FindProperty("attackStrategyComponent").objectReferenceValue = strategy;
            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[EnemyMigration] '{prefabName}' migration complete. Strategy='{typeof(TStrategy).Name}'.");
        }
    }

    [MenuItem("Tools/Enemy System/Delete Old Enemy Scripts (after verification)")]
    public static void DeleteOldScripts()
    {
        if (!EditorUtility.DisplayDialog(
            "Delete Old Scripts",
            "This will permanently delete:\n" +
            "• EnemyBase.cs\n• BasicEnemy.cs\n• HeavyEnemy.cs\n• RangedEnemy.cs\n\n" +
            "Only do this after confirming the migrated prefabs work in Play Mode!",
            "Delete", "Cancel"))
            return;

        string[] toDelete =
        {
            "Assets/_Game/Scripts/Enemies/EnemyBase.cs",
            "Assets/_Game/Scripts/Enemies/BasicEnemy.cs",
            "Assets/_Game/Scripts/Enemies/HeavyEnemy.cs",
            "Assets/_Game/Scripts/Enemies/RangedEnemy.cs",
        };

        foreach (string path in toDelete)
        {
            if (File.Exists(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[EnemyMigration] Deleted {path}");
            }
            else
            {
                Debug.LogWarning($"[EnemyMigration] Not found (already deleted?): {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyMigration] Old scripts removed.");
    }
}
