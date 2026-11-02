using System;
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
    private List<GameObject> activePickups = new List<GameObject>();

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

    public static event Action OnRoomCompleted;
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

    public void SetRewards(float currencyMultiplier = 1f, int currency = -1, float healthDropChance = -1f, float healAmount = -1f)
    {
        this.currencyMultiplier = currencyMultiplier;
        currencyOverride = currency;
        healthDropChanceOverride = healthDropChance;
        healAmountOverride = healAmount;
    }

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

    private bool TrySpawnEnemy(GameObject prefab)
    {
        if (TryGetEnemyPoint(out Vector3 spawnPos))
        {
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            activeEnemies.Add(enemy);

            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                health.OnDeath.AddListener(() => OnEnemyDeath(enemy));
            }
            return true;
        }
        return false;
    }

    private void OnEnemyDeath(GameObject enemy)
    {
        if (roomCleared) return;

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

        if (currentWaveIndex < currentConfig.maxWaves)
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

        int currency = currencyOverride >= 0
            ? currencyOverride
            : Mathf.RoundToInt(roomConfig.expectedCurrency * currencyMultiplier);
        float healthDropChance = healthDropChanceOverride >= 0f ? healthDropChanceOverride : roomConfig.healthDropChance;

        for (int i = 0; i < currency; i++)
        {
            if (coinPrefab != null)
            {
                GameObject coinObj = SpawnPickup(coinPrefab);
                if (coinObj != null)
                {
                    Coin coin = coinObj.GetComponent<Coin>();
                    if (coin != null)
                    {
                        coin.OnCollected += () => OnPickupCollected(coinObj);
                    }
                }
            }
        }

        if (Random.value < healthDropChance)
        {
            if (healthDropPrefab != null)
            {
                GameObject healthObj = SpawnPickup(healthDropPrefab);
                if (healthObj != null)
                {
                    HealthPickup health = healthObj.GetComponent<HealthPickup>();
                    if (health != null)
                    {
                        if (healAmountOverride >= 0f) health.HealAmount = healAmountOverride;
                        health.OnCollected += () => OnPickupCollected(healthObj);
                    }
                }
            }
        }

        if (activePickups.Count == 0)
        {
            OnRoomCompleted?.Invoke();
        }
    }

    private GameObject SpawnPickup(GameObject prefab)
    {
        if (!TryGetPickupPoint(out Vector3 ground)) return null;

        Vector3 rest = ground + Vector3.up * pickupLandingHeight;
        GameObject pickup = Instantiate(prefab, rest, Quaternion.identity);
        activePickups.Add(pickup);
        if (pickup.TryGetComponent(out PickupDropAnimation drop))
        {
            drop.Drop(rest, ground.y);
        }
        return pickup;
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

    private void OnDestroy()
    {
        foreach (GameObject pickup in activePickups)
        {
            if (pickup != null) Destroy(pickup);
        }
        activePickups.Clear();
    }

    private void OnPickupCollected(GameObject pickup)
    {
        if (this == null) return;

        activePickups.Remove(pickup);
        if (activePickups.Count == 0)
        {
            OnRoomCompleted?.Invoke();
        }
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
