using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.IO;

/// <summary>
/// One-click migration tool.
/// Menu: Tools > Enemy System > Migrate Prefabs to Component-Strategy Pattern
///
/// What it does:
///   1. Creates EnemyData ScriptableObject assets (Slimo / Heavy / Ranged) under
///      Assets/_DimaAssets/EnemyData/ if they don't already exist.
///   2. For each of the three enemy prefabs it:
///        - Removes the old monolithic script (BasicEnemy / HeavyEnemy / RangedEnemy).
///        - Removes the legacy CharacterController (Enemy.cs requires NavMeshAgent).
///        - Adds NavMeshAgent (if not already present).
///        - Adds the new Enemy orchestrator.
///        - Adds the correct IEnemyAttackStrategy component.
///        - Wires EnemyData + strategy references on the Enemy component.
///        - Saves the prefab.
/// </summary>
public static class EnemyMigrationTool
{
    private const string DataDir = "Assets/_DimaAssets/EnemyData";
    private const string PrefabDir = "Assets/Prefabs";

    [MenuItem("Tools/Enemy System/Migrate Prefabs to Component-Strategy Pattern")]
    public static void MigrateAll()
    {
        if (!EditorUtility.DisplayDialog(
            "Enemy System Migration",
            "This will:\n" +
            "• Create EnemyData assets in Assets/_DimaAssets/EnemyData/\n" +
            "• Rewire Basic Enemy, Heavy Enemy, Ranged Enemy prefabs\n" +
            "• Remove old BasicEnemy/HeavyEnemy/RangedEnemy scripts\n\n" +
            "Prefabs are saved automatically. Continue?",
            "Migrate", "Cancel"))
            return;

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "../" + DataDir));
        AssetDatabase.Refresh();

        // --- Create data assets --------------------------------------------------
        EnemyData slimoData  = GetOrCreateData("SlimoData",   maxHealth: 50f,  damage: 10f, speed: 5f,  alertRange: 10f, attackRange: 3f,  alertDuration: 1f, roamRadius: 10f, idleDuration: 1.5f, patrolSpeedMult: 0.5f, windupDuration: 0.25f, attackCooldown: 2f,   leashMult: 1.5f);
        EnemyData heavyData  = GetOrCreateData("HeavyData",   maxHealth: 80f,  damage: 30f, speed: 2f,  alertRange: 10f, attackRange: 2.5f, alertDuration: 1f, roamRadius: 8f,  idleDuration: 2f,   patrolSpeedMult: 0.5f, windupDuration: 0.5f,  attackCooldown: 1.5f, leashMult: 1.5f);
        EnemyData rangedData = GetOrCreateData("RangedData",  maxHealth: 25f,  damage: 30f, speed: 5f,  alertRange: 30f, attackRange: 17f, alertDuration: 1f, roamRadius: 12f, idleDuration: 1.5f, patrolSpeedMult: 0.5f, windupDuration: 0.5f,  attackCooldown: 2f,   leashMult: 2f);

        AssetDatabase.SaveAssets();
        Debug.Log("[EnemyMigration] EnemyData assets created/verified.");

        // --- Migrate prefabs ------------------------------------------------------
        MigratePrefab<BasicEnemy,  DashAttack>  ("Basic Enemy",  slimoData);
        MigratePrefab<HeavyEnemy,  MeleeAttack> ("Heavy Enemy",  heavyData);
        MigratePrefab<RangedEnemy, RangedAttack>("Ranged Enemy", rangedData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Migration Complete",
            "All 3 prefabs have been migrated to the Component-Strategy pattern.\n\n" +
            "Next steps:\n" +
            "1. Open each prefab and verify the Enemy + strategy + EnemyData are wired correctly.\n" +
            "2. Delete EnemyBase.cs, BasicEnemy.cs, HeavyEnemy.cs, RangedEnemy.cs when ready.\n" +
            "3. Bake NavMesh (the prefabs now use NavMeshAgent instead of CharacterController).",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private static EnemyData GetOrCreateData(
        string assetName,
        float maxHealth, float damage, float speed, float alertRange, float attackRange,
        float alertDuration, float roamRadius, float idleDuration,
        float patrolSpeedMult, float windupDuration, float attackCooldown, float leashMult)
    {
        string path = $"{DataDir}/{assetName}.asset";
        EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (existing != null) return existing;

        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.maxHealth            = maxHealth;
        data.damage               = damage;
        data.speed                = speed;
        data.alertRange           = alertRange;
        data.attackRange          = attackRange;
        data.alertDuration        = alertDuration;
        data.roamRadius           = roamRadius;
        data.idleDuration         = idleDuration;
        data.patrolSpeedMultiplier = patrolSpeedMult;
        data.windupDuration       = windupDuration;
        data.attackCooldown       = attackCooldown;
        data.chaseLeashMultiplier = leashMult;

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[EnemyMigration] Created {path}");
        return data;
    }

    /// <summary>
    /// Generic prefab migrator.
    /// TOld = the old monolithic script to remove.
    /// TStrategy = the IEnemyAttackStrategy MonoBehaviour to add.
    /// </summary>
    private static void MigratePrefab<TOld, TStrategy>(string prefabName, EnemyData data)
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

        // Open the prefab for editing
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            // 1. Remove old enemy script
            var oldScript = root.GetComponent<TOld>();
            if (oldScript != null)
            {
                Object.DestroyImmediate(oldScript, true);
                Debug.Log($"[EnemyMigration] Removed {typeof(TOld).Name} from '{prefabName}'.");
            }

            // 2. Remove CharacterController (new system uses NavMeshAgent)
            var cc = root.GetComponent<CharacterController>();
            if (cc != null)
            {
                Object.DestroyImmediate(cc, true);
                Debug.Log($"[EnemyMigration] Removed CharacterController from '{prefabName}'.");
            }

            // 3. Ensure NavMeshAgent is present
            if (root.GetComponent<NavMeshAgent>() == null)
            {
                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius          = 0.5f;
                agent.height          = 2f;
                agent.speed           = data.speed;
                agent.angularSpeed    = 180f;
                agent.acceleration    = 8f;
                agent.stoppingDistance = 0f;
                agent.autoBraking     = true;
                Debug.Log($"[EnemyMigration] Added NavMeshAgent to '{prefabName}'.");
            }

            // 4. Ensure Health is present (should already be there, but just in case)
            if (root.GetComponent<Health>() == null)
                root.AddComponent<Health>();

            // 5. Add the strategy component (if not already present)
            TStrategy strategy = root.GetComponent<TStrategy>();
            if (strategy == null)
            {
                strategy = root.AddComponent<TStrategy>();
                Debug.Log($"[EnemyMigration] Added {typeof(TStrategy).Name} to '{prefabName}'.");
            }

            // 6. Configure RangedAttack defaults if applicable
            if (strategy is RangedAttack ranged)
            {
                // Keep existing projectile prefab reference if already set
                if (ranged.idealDistance == 0f)
                    ranged.idealDistance = 15f;
            }

            // 7. Add the Enemy orchestrator (if not already present)
            Enemy enemy = root.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = root.AddComponent<Enemy>();
                Debug.Log($"[EnemyMigration] Added Enemy orchestrator to '{prefabName}'.");
            }

            // 8. Wire references (using SerializedObject so Unity serialises them)
            SerializedObject so = new SerializedObject(enemy);
            so.FindProperty("data").objectReferenceValue = data;
            // attackStrategyComponent is the [SerializeReference] MonoBehaviour slot
            so.FindProperty("attackStrategyComponent").objectReferenceValue = strategy;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[EnemyMigration] '{prefabName}' migration complete. Data='{data.name}', Strategy='{typeof(TStrategy).Name}'.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Cleanup helper (run separately after verifying everything works)
    // ─────────────────────────────────────────────────────────────────────────────

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
            "Assets/_DimaAssets/_Scripts/EnemyBase.cs",
            "Assets/_DimaAssets/_Scripts/BasicEnemy.cs",
            "Assets/_DimaAssets/_Scripts/HeavyEnemy.cs",
            "Assets/_DimaAssets/_Scripts/RangedEnemy.cs",
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
