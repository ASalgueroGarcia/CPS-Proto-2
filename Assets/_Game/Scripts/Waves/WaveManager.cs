using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyPrefabConfig
    {
        public EnemyType type;
        public GameObject prefab;
        public int cost;
    }

    [Header("Enemy Configuration")]
    [SerializeField] private List<EnemyPrefabConfig> enemyConfigs;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(70f, 30f);

    [Header("Safe Zones")]
    // NOTE: The door transition and completion triggers are placeholders and might not work consistently until room maps match Notion structure.
    [SerializeField] private Vector2 doorPositionXZ = new Vector2(0f, 13.1f);
    [SerializeField] private float enemySafeRadius = 6f;
    [SerializeField] private float pickupClearance = 0.45f;
    [SerializeField] private float pickupLandingHeight = 0.1f;

    [Header("Raycast Settings")]
    [SerializeField] private float raycastHeight = 20f;
    [SerializeField] private float maxNavMeshDistance = 2f;

    [Header("Room Setup")]
    public RoomType currentRoomType = RoomType.Medium;
    public RoomConfigData config;
    [SerializeField] private RoomConfigLibrary roomConfigLibrary;

    private int currentWaveIndex = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool roomCleared = false;
    private bool roomCompleted = false;
    // Pickups the room waits for before OnRoomCompleted.
    private List<GameObject> activePickups = new List<GameObject>();
    // Every pickup this room spawned, so OnDestroy can clean up what's left on the floor.
    private List<GameObject> spawnedPickups = new List<GameObject>();

    [Header("Encounters (GDD In-Run)")]
    [Tooltip("Weighted, capped, scene-gated enemy table + reward rules (Data/Rooms). Used once the map passes the room's context; empty = legacy uniform spawning from Enemy Configs.")]
    [SerializeField] private EncounterTable encounterTable;
    private EncounterContext encounterContext;
    private bool hasEncounterContext;
    private int[] encounterCounts;
    private int roomBudget;
    private int roomBudgetLeft;
    private List<EncounterTable.Entry> roomPlan;
    private int roomPlanIndex;
    private const int SpawnAttempts = 10;
    // Enemies made by a spawner (Deck of Cards soldiers): they keep the room open but drop nothing.
    private readonly HashSet<GameObject> noRewardEnemies = new HashSet<GameObject>();

    [Header("Rewards")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject healthDropPrefab;

    private int slimoCountInCurrentWave = 0;
    private int heavyCountInCurrentWave = 0;
    private int totalEnemiesInCurrentWave = 0;

    private bool hasInitialized = false;
    private bool held;

    private float currencyMultiplier = 1f;
    private int currencyOverride = -1;
    private float healthDropChanceOverride = -1f;
    private float healAmountOverride = -1f;

    /// <summary>
    /// Raised once the room is cleared and every reward has been picked up (right away if
    /// none spawned). UIManager listens and shows the end-of-level canvas.
    /// </summary>
    public static event Action OnRoomCompleted;

    /// <summary>
    /// Raised once, the moment the room's last enemy dies - before its rewards are picked up.
    /// The fight is won here: MapBehaviour saves a boss victory on it.
    /// </summary>
    public static event Action OnEnemiesCleared;

    public void SetRoomType(RoomType type)
    {
        if (hasInitialized)
        {
            Debug.LogWarning($"[WaveManager] SetRoomType({type}) called after room already initialized.", this);
            return;
        }

        currentRoomType = type;
    }

    /// <summary>
    /// Map-side reward overrides for rooms the encounter rules don't price (Treasure, Rest).
    /// A currency or health chance set here wins over the encounter rules.
    /// </summary>
    public void SetRewards(float currencyMultiplier = 1f, int currency = -1, float healthDropChance = -1f, float healAmount = -1f)
    {
        this.currencyMultiplier = currencyMultiplier;
        currencyOverride = currency;
        healthDropChanceOverride = healthDropChance;
        healAmountOverride = healAmount;
    }

    /// <summary>
    /// Tells the room where it sits on the run map: scene in the act, node kind, First Spot,
    /// room index inside the node. Drives the Threat Budget, enemy gating/weights/caps and the
    /// reward formulas. Same timing as SetRoomType: call it before the room initializes.
    /// Without it the room plays as before: tier budget, uniform spawning, tier rewards.
    /// </summary>
    public void SetEncounterContext(EncounterContext context)
    {
        if (hasInitialized)
        {
            Debug.LogWarning("[WaveManager] SetEncounterContext() called after the room already initialized. Ignored - set it before Start() runs.", this);
            return;
        }

        encounterContext = context;
        hasEncounterContext = true;
    }

    // The GDD rules need the room's place on the map. Without it, and in Shop/Treasure rooms, the room
    // plays as before: tier budget, uniform spawning, tier rewards.
    private bool UsesEncounterRules => encounterTable != null && hasEncounterContext
        && currentRoomType != RoomType.Shop && currentRoomType != RoomType.Treasure;

    // The Final Fight is the boss's own arena: until boss prefabs exist it keeps its tier's spawning,
    // and only its rewards follow the GDD.
    private bool UsesEncounterSpawning => UsesEncounterRules && encounterContext.nodeKind != EncounterNodeKind.FinalFight;

    private void Start()
    {
        hasInitialized = true;
        if (!held) InitializeRoom();
    }

    public void HoldUntilBegin()
    {
        held = !hasInitialized;
    }

    public void Begin()
    {
        if (!held) return;
        held = false;
        InitializeRoom();
    }

    private void InitializeRoom()
    {
        if (currentRoomType == RoomType.Shop || currentRoomType == RoomType.Treasure)
        {
            roomCleared = true;
            if (currentRoomType == RoomType.Treasure) SpawnRewards();
            return;
        }

        SpawnWave();
    }

    private void SpawnWave()
    {
        if (roomCleared) return;

        currentWaveIndex++;
        if (roomConfigLibrary == null)
        {
            Debug.LogError("[WaveManager] RoomConfigLibrary is not assigned.", this);
            return;
        }

        config = roomConfigLibrary.Get(currentRoomType);
        if (config == null) return;

        if (UsesEncounterSpawning)
        {
            SpawnEncounterWave();
            if (activeEnemies.Count == 0 && !roomCleared) CheckWaveCompletion();
            return;
        }

        int budget = config.budget;
        List<EnemyType> pool = config.enemyPool;

        if (currentWaveIndex == 3)
        {
            pool = new List<EnemyType> { EnemyType.Slimo };
        }

        int currentSpent = 0;
        slimoCountInCurrentWave = 0;
        heavyCountInCurrentWave = 0;
        totalEnemiesInCurrentWave = 0;

        while (currentSpent < budget)
        {
            EnemyType type = pool[Random.Range(0, pool.Count)];
            List<EnemyPrefabConfig> matchingConfigs = enemyConfigs.FindAll(c => c.type == type);
            EnemyPrefabConfig prefabConfig = matchingConfigs.Count > 0 ? matchingConfigs[Random.Range(0, matchingConfigs.Count)] : null;

            if (prefabConfig == null) break;

            if (currentSpent + prefabConfig.cost <= budget)
            {
                if (TrySpawnEnemy(prefabConfig.prefab))
                {
                    currentSpent += prefabConfig.cost;
                    totalEnemiesInCurrentWave++;
                    if (type == EnemyType.Slimo) slimoCountInCurrentWave++;
                    if (type == EnemyType.Heavy) heavyCountInCurrentWave++;
                }
            }
            else
            {
                EnemyPrefabConfig cheapest = enemyConfigs.Find(c => c.cost == 1);
                if (cheapest != null && currentSpent + cheapest.cost <= budget)
                {
                    if (TrySpawnEnemy(cheapest.prefab))
                    {
                        currentSpent += cheapest.cost;
                        totalEnemiesInCurrentWave++;
                        if (cheapest.type == EnemyType.Slimo) slimoCountInCurrentWave++;
                    }
                    else break;
                }
                else break;
            }
        }

        if (activeEnemies.Count == 0 && !roomCleared)
        {
            CheckWaveCompletion();
        }
    }

    /// <summary>
    /// GDD rules: the room's Threat Budget (from the map context) buys weighted, capped, scene-gated enemies from the EncounterTable until nothing
    /// affordable is left. The whole room is bought at once - splitting the budget first would
    /// shut a cost-4 Deck out of every 3-point wave - then dealt over the tier's waves.
    /// </summary>
    private void SpawnEncounterWave()
    {
        int waves = Mathf.Max(1, config.maxWaves);
        if (encounterCounts == null)
        {
            encounterCounts = new int[encounterTable.enemies.Count];
            roomBudget = encounterTable.RoomBudget(encounterContext);
            roomBudgetLeft = roomBudget;
            roomPlan = encounterTable.Plan(roomBudget, encounterContext, encounterCounts, () => Random.value);
            roomPlanIndex = 0;
        }

        // This wave's share of what is still planned; the last wave takes the rest.
        bool lastWave = currentWaveIndex >= waves;
        int plannedLeft = 0;
        for (int i = roomPlanIndex; i < roomPlan.Count; i++) plannedLeft += roomPlan[i].cost;
        int waveTarget = lastWave ? plannedLeft : Mathf.CeilToInt(plannedLeft / (float)(waves - currentWaveIndex + 1));

        var wavePicks = new List<EncounterTable.Entry>();
        for (int taken = 0; roomPlanIndex < roomPlan.Count && (taken < waveTarget || wavePicks.Count == 0); roomPlanIndex++)
        {
            wavePicks.Add(roomPlan[roomPlanIndex]);
            taken += roomPlan[roomPlanIndex].cost;
        }

        int spent = SpawnPicks(wavePicks);
        roomBudgetLeft -= spent;

        // Budget lost to failed spawns is bought again once the plan is dealt, within the same caps.
        if ((lastWave || roomPlanIndex >= roomPlan.Count) && roomBudgetLeft > 0)
        {
            int topUp = SpawnPicks(encounterTable.Plan(roomBudgetLeft, encounterContext, encounterCounts, () => Random.value));
            roomBudgetLeft -= topUp;
            spent += topUp;
        }

        Debug.Log($"[WaveManager] Wave {currentWaveIndex}/{waves}: spent {spent} (room budget {roomBudget}, left {roomBudgetLeft}, scene {encounterContext.scene}).");
    }

    /// <summary>Spawns the picks; a pick that finds no spawn point is given back to its cap. Returns the cost spent.</summary>
    private int SpawnPicks(List<EncounterTable.Entry> picks)
    {
        int spent = 0;
        foreach (EncounterTable.Entry pick in picks)
        {
            // A random point can miss the NavMesh (holes, walls): retry before giving the enemy up.
            bool spawned = false;
            for (int attempt = 0; attempt < SpawnAttempts && !spawned; attempt++)
            {
                spawned = TrySpawnEnemy(pick.prefab);
            }

            if (spawned)
            {
                spent += pick.cost;
            }
            else
            {
                encounterCounts[encounterTable.IndexOf(pick)]--;
            }
        }

        return spent;
    }

    private bool TrySpawnEnemy(GameObject prefab)
    {
        if (TryGetEnemyPoint(out Vector3 spawnPos))
        {
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            RegisterEnemy(enemy, true);
            return true;
        }
        return false;
    }

    private void RegisterEnemy(GameObject enemy, bool dropsRewards)
    {
        activeEnemies.Add(enemy);
        if (!dropsRewards) noRewardEnemies.Add(enemy);

        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            health.OnDeath.AddListener(() => OnEnemyDeath(enemy));
        }

        // A Deck of Cards keeps the room open: whatever it makes is tracked like a room enemy.
        foreach (IEnemySpawner spawner in enemy.GetComponentsInChildren<IEnemySpawner>(true))
        {
            spawner.OnEnemySpawned += minion =>
            {
                if (this != null && !roomCleared && minion != null) RegisterEnemy(minion, false);
            };
        }
    }

    private void OnEnemyDeath(GameObject enemy)
    {
        if (roomCleared) return;
        if (!activeEnemies.Contains(enemy)) return;

        if (UsesEncounterRules && !noRewardEnemies.Contains(enemy)
            && Random.value < encounterTable.KillButtonChance(encounterContext.nodeKind))
        {
            SpawnKillCoin(enemy != null ? enemy.transform.position : transform.position);
        }

        noRewardEnemies.Remove(enemy);
        activeEnemies.Remove(enemy);
        if (activeEnemies.Count == 0)
        {
            CheckWaveCompletion();
        }
    }

    private void CheckWaveCompletion()
    {
        RoomConfigData currentConfig = roomConfigLibrary != null ? roomConfigLibrary.Get(currentRoomType) : null;
        if (currentConfig == null) return;

        // With an EncounterTable, waves past the room's plan would only make the player wait 2 s for nothing.
        bool morePlanned = roomPlan == null || roomPlanIndex < roomPlan.Count;
        if (currentWaveIndex < currentConfig.maxWaves && morePlanned)
        {
            Invoke(nameof(SpawnWave), 2f);
        }
        else
        {
            OnRoomCleared();
        }
    }

    private void OnRoomCleared()
    {
        if (roomCleared) return;
        roomCleared = true;
        OnEnemiesCleared?.Invoke();

        CancelInvoke(nameof(SpawnWave));
        SpawnRewards();
    }

    private void SpawnRewards()
    {
        RoomConfigData roomConfig = roomConfigLibrary != null ? roomConfigLibrary.Get(currentRoomType) : null;
        if (roomConfig == null) return;

        // Map overrides (Treasure, Rest) first; then the GDD rules; else the tier's numbers.
        bool useEncounterRewards = UsesEncounterRules;
        EncounterNodeKind nodeKind = encounterContext.nodeKind;
        int currency = currencyOverride >= 0 ? currencyOverride
            : useEncounterRewards ? encounterTable.RoomClearButtons(roomBudget, nodeKind)
            : Mathf.RoundToInt(roomConfig.expectedCurrency * currencyMultiplier);
        float healthDropChance = healthDropChanceOverride >= 0f ? healthDropChanceOverride
            : useEncounterRewards ? encounterTable.HealthDropChance(nodeKind)
            : roomConfig.healthDropChance;

        for (int i = 0; i < currency; i++)
        {
            if (coinPrefab == null) break;
            TrackCoin(SpawnPickup(coinPrefab));
        }

        if (Random.value < healthDropChance && healthDropPrefab != null)
        {
            GameObject healthObj = SpawnPickup(healthDropPrefab);
            if (healthObj != null)
            {
                HealthPickup health = healthObj.GetComponent<HealthPickup>();
                if (health != null && healAmountOverride >= 0f) health.HealAmount = healAmountOverride;

                // GDD: a combat room's health drop stays on the floor until the player leaves, so the
                // room doesn't wait for it. Rooms outside the GDD rules (Treasure, Rest) still do.
                if (!useEncounterRewards)
                {
                    activePickups.Add(healthObj);
                    if (health != null) health.OnCollected += () => OnPickupCollected(healthObj);
                }
            }
        }

        // GDD: Buttons (room clear + kill drops still on the floor) fly to the player after the clear.
        if (useEncounterRewards && activePickups.Count > 0 && isActiveAndEnabled)
        {
            StartCoroutine(FlyButtonsToPlayer(encounterTable.buttonFlyDelay));
        }

        if (activePickups.Count == 0)
        {
            CompleteRoom();
        }
    }

    private GameObject SpawnPickup(GameObject prefab)
    {
        if (!TryGetPickupPoint(out Vector3 ground)) return null;
        return DropPickup(prefab, ground);
    }

    // A kill drop lands next to the enemy that dropped it, not somewhere random in the room.
    private void SpawnKillCoin(Vector3 near)
    {
        if (coinPrefab == null) return;
        if (!TryGetValidPointNear(near, 0.5f, out Vector3 ground)) return;
        TrackCoin(DropPickup(coinPrefab, ground));
    }

    private GameObject DropPickup(GameObject prefab, Vector3 ground)
    {
        Vector3 rest = ground + Vector3.up * pickupLandingHeight;
        GameObject pickup = Instantiate(prefab, rest, Quaternion.identity);
        spawnedPickups.Add(pickup);
        if (pickup.TryGetComponent(out PickupDropAnimation drop))
        {
            drop.Drop(rest, ground.y);
        }
        return pickup;
    }

    private void TrackCoin(GameObject coinObj)
    {
        if (coinObj == null) return;

        activePickups.Add(coinObj);
        Coin coin = coinObj.GetComponent<Coin>();
        if (coin != null)
        {
            coin.OnCollected += () => OnPickupCollected(coinObj);
        }
    }

    private IEnumerator FlyButtonsToPlayer(float delay)
    {
        yield return new WaitForSeconds(delay);

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) yield break;

        foreach (GameObject pickup in activePickups)
        {
            if (pickup != null && pickup.TryGetComponent(out Coin coin)) coin.FlyTo(player.transform, encounterTable.buttonFlySpeed);
        }
    }

    private bool TryGetEnemyPoint(out Vector3 result)
    {
        for (int i = 0; i < 20; i++)
        {
            if (TryGetRandomPoint(out result)
                && Vector2.Distance(new Vector2(result.x, result.z), doorPositionXZ) >= enemySafeRadius)
            {
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private bool TryGetPickupPoint(out Vector3 result)
    {
        for (int i = 0; i < 20; i++)
        {
            if (TryGetRandomPoint(out result)
                && !Physics.CheckSphere(result + Vector3.up * (pickupClearance + 0.05f), pickupClearance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private bool TryGetValidPointNear(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 origin = new Vector3(center.x + randomCircle.x, center.y + raycastHeight, center.z + randomCircle.y);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2f)
                && NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, maxNavMeshDistance, NavMesh.AllAreas))
            {
                result = navHit.position;
                return true;
            }
        }

        result = center;
        return false;
    }

    // Rewards spawn at the scene root, so they would outlive the room: an uncollected coin picked
    // up in the next room would complete this one over there (end-of-level screen, boss "win").
    private void OnDestroy()
    {
        foreach (GameObject pickup in spawnedPickups)
        {
            if (pickup != null) Destroy(pickup);
        }

        activePickups.Clear();
        spawnedPickups.Clear();
    }

    private void OnPickupCollected(GameObject pickup)
    {
        if (this == null) return;

        activePickups.Remove(pickup);
        // Kill-drop Buttons can be picked up mid-fight: only an empty floor after the clear completes the room.
        if (roomCleared && activePickups.Count == 0)
        {
            CompleteRoom();
        }
    }

    private void CompleteRoom()
    {
        if (roomCompleted) return;
        roomCompleted = true;
        OnRoomCompleted?.Invoke();
    }

    private bool TryGetRandomPoint(out Vector3 result)
    {
        float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomY = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        Vector3 origin = new Vector3(transform.position.x + randomX, transform.position.y + raycastHeight, transform.position.z + randomY);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2f))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, maxNavMeshDistance, NavMesh.AllAreas))
            {
                result = navHit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0, spawnAreaSize.y));
    }
}
