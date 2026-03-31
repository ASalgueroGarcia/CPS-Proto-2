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
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(30f, 30f);
    
    [Header("Raycast Settings")]
    [Tooltip("How high above the spawner's Y position to start the raycast.")]
    [SerializeField] private float raycastHeight = 20f;
    [Tooltip("The maximum distance to search for a valid NavMesh point from our ground hit.")]
    [SerializeField] private float maxNavMeshDistance = 2f;

    [Header("Room Setup")]
    public RoomType currentRoomType = RoomType.Medium;
    public RoomConfig config;
    private int currentWaveIndex = 0;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool roomCleared = false;
    private List<GameObject> activePickups = new List<GameObject>();

    [Header("Rewards Placeholders")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject healthDropPrefab;

    // Track current wave composition for modifiers
    private int slimoCountInCurrentWave = 0;
    private int heavyCountInCurrentWave = 0;
    private int totalEnemiesInCurrentWave = 0;

    private void Start()
    {
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
        if (roomCleared) return; // Safety check: don't spawn waves if room is already cleared

        currentWaveIndex++;
        config = RoomConfigs.Get(currentRoomType);
        
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
            EnemyPrefabConfig prefabConfig = enemyConfigs.Find(c => c.type == type);

            if (prefabConfig == null) break;

            if (currentSpent + prefabConfig.cost <= budget)
            {
                if (TrySpawnEnemy(prefabConfig.prefab))
                {
                    currentSpent += prefabConfig.cost;
                    Debug.Log($"Spawned {type} (Cost: {prefabConfig.prefab.name}). Remaining Budget: {budget - currentSpent}");
                    totalEnemiesInCurrentWave++;
                    if (type == EnemyType.Slimo) slimoCountInCurrentWave++;
                    if (type == EnemyType.Heavy) heavyCountInCurrentWave++;
                }
            }
            else
            {
                var cheapest = enemyConfigs.Find(c => c.cost == 1);
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
        
        // If we failed to spawn any enemies for some reason, check if room is cleared
        if (activeEnemies.Count == 0 && !roomCleared)
        {
            CheckWaveCompletion();
        }
    }

    private bool TrySpawnEnemy(GameObject prefab)
    {
        Vector3 spawnPos;
        if (TryGetRandomPoint(out spawnPos))
        {
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            activeEnemies.Add(enemy);
            
            Health h = enemy.GetComponent<Health>();
            if (h != null)
            {
                h.OnDeath.AddListener(() => OnEnemyDeath(enemy));
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
        RoomConfig currentConfig = RoomConfigs.Get(currentRoomType);
        
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
        Debug.Log($"<color=green>Room Cleared!</color> Room Type: {currentRoomType}");
        
        // Cancel any pending wave spawns just in case
        CancelInvoke(nameof(SpawnWave));
        
        SpawnRewards();
    }

    private void SpawnRewards()
    {
        RoomConfig roomConfig = RoomConfigs.Get(currentRoomType);
        
        // Use transform.position as the base center
        Vector3 centerPos = transform.position;

        // Spawn currency
        for (int i = 0; i < roomConfig.expectedCurrency; i++)
        {
            if (coinPrefab != null)
            {
                if (TryGetValidPointNear(centerPos, 2f, out Vector3 dropPos))
                {
                    GameObject coinObj = Instantiate(coinPrefab, dropPos + Vector3.up * 0.5f, Quaternion.identity);
                    activePickups.Add(coinObj);
                    Coin coin = coinObj.GetComponent<Coin>();
                    if (coin != null)
                    {
                        coin.OnCollected += () => OnPickupCollected(coinObj);
                    }
                }
            }
        }

        // Health drop
        if (Random.value < roomConfig.healthDropChance)
        {
            if (healthDropPrefab != null)
            {
                if (TryGetValidPointNear(centerPos, 1.5f, out Vector3 healthPos))
                {
                    GameObject healthObj = Instantiate(healthDropPrefab, healthPos + Vector3.up * 0.5f, Quaternion.identity);
                    activePickups.Add(healthObj);
                    HealthPickup health = healthObj.GetComponent<HealthPickup>();
                    if (health != null)
                    {
                        health.OnCollected += () => OnPickupCollected(healthObj);
                    }
                }
            }
        }

        // If no pickups were spawned, show EoLCanvas immediately
        if (activePickups.Count == 0)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowEoLCanvas();
        }
    }

    private bool TryGetValidPointNear(Vector3 center, float radius, out Vector3 result)
    {
        // Try up to 10 times to find a valid spot
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 origin = new Vector3(center.x + randomCircle.x, center.y + raycastHeight, center.z + randomCircle.y);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2))
            {
                // Check if we hit ground (you might want to check layer/tag here, 
                // but for now we follow TryGetRandomPoint logic)
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, maxNavMeshDistance, NavMesh.AllAreas))
                {
                    result = navHit.position;
                    return true;
                }
            }
        }

        result = center;
        return false;
    }

    private void OnPickupCollected(GameObject pickup)
    {
        activePickups.Remove(pickup);
        if (activePickups.Count == 0)
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowEoLCanvas();
        }
    }

    private bool TryGetRandomPoint(out Vector3 result)
    {
        float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomY = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        Vector3 origin = new Vector3(transform.position.x + randomX, transform.position.y + raycastHeight, transform.position.z + randomY);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2))
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

public class RoomConfig
{
    public int budget;
    public float baseWaveChance;
    public int maxWaves;
    public List<EnemyType> enemyPool;
    public int expectedCurrency;
    public float healthDropChance;
}

public static class RoomConfigs
{
    private static Dictionary<RoomType, RoomConfig> configs = new Dictionary<RoomType, RoomConfig>
    {
        { RoomType.Entrance, new RoomConfig { budget = 4, baseWaveChance = 0f, maxWaves = 1, enemyPool = new List<EnemyType>{EnemyType.Slimo}, expectedCurrency = 1, healthDropChance = 0f }},
        { RoomType.Medium, new RoomConfig { budget = 6, baseWaveChance = 0.15f, maxWaves = 2, enemyPool = new List<EnemyType>{EnemyType.Slimo, EnemyType.Ranged}, expectedCurrency = 2, healthDropChance = 0.1f }},    
        { RoomType.MediumHard, new RoomConfig { budget = 9, baseWaveChance = 0.30f, maxWaves = 2, enemyPool = new List<EnemyType>{EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy}, expectedCurrency = 2, healthDropChance = 0.2f }},
        { RoomType.Hard, new RoomConfig { budget = 12, baseWaveChance = 0.45f, maxWaves = 3, enemyPool = new List<EnemyType>{EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy}, expectedCurrency = 3, healthDropChance = 0.3f }},
        { RoomType.MiniBoss, new RoomConfig { budget = 16, baseWaveChance = 0.60f, maxWaves = 3, enemyPool = new List<EnemyType>{EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy}, expectedCurrency = 4, healthDropChance = 0.6f }},
        { RoomType.Boss, new RoomConfig { budget = 12, baseWaveChance = 1.0f, maxWaves = 3, enemyPool = new List<EnemyType>{EnemyType.Slimo, EnemyType.Ranged, EnemyType.Heavy}, expectedCurrency = 6, healthDropChance = 1.0f }},
        { RoomType.Shop, new RoomConfig { budget = 0, baseWaveChance = 0, maxWaves = 0, enemyPool = new List<EnemyType>(), expectedCurrency = 0, healthDropChance = 0 }},
        { RoomType.Treasure, new RoomConfig { budget = 0, baseWaveChance = 0, maxWaves = 0, enemyPool = new List<EnemyType>(), expectedCurrency = 0, healthDropChance = 1.0f }}
    };

    public static RoomConfig Get(RoomType type)
    {
        return configs[type];
    }
}
