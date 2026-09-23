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

    private bool hasInitialized = false;

    // Cached player refs for dependency injection into spawned enemies (implementation_plan_enem).
    private Transform _playerTransform;
    private Health _playerHealth;

    /// <summary>
    /// Sets the difficulty tier for this room. Called by SceneController immediately
    /// after the room prefab is instantiated, which is before Start() runs.
    /// Setting it any later has no effect - the first wave has already been spawned.
    /// </summary>
    public void SetRoomType(RoomType type)
    {
        if (hasInitialized)
        {
            Debug.LogWarning($"[WaveManager] SetRoomType({type}) called after the room already initialized as {currentRoomType}. Ignored - set the tier before Start() runs.", this);
            return;
        }

        currentRoomType = type;
    }

    private void Start()
    {
        hasInitialized = true;

        // Cache the player once; spawned enemies get the references injected (no per-enemy searching).
        var playerObj = FindFirstObjectByType<PlayerFSM>();
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _playerHealth = playerObj.GetComponent<Health>();
        }

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

            // Dependency injection: the enemy never searches for the player itself.
            Enemy enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.SetTarget(_playerTransform, _playerHealth);
            }
            
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

