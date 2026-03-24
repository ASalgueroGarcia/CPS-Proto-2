using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("List of different enemy prefabs to spawn.")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(30f, 70f);
    
    [Header("Raycast Settings")]
    [Tooltip("How high above the spawner's Y position to start the raycast.")]
    [SerializeField] private float raycastHeight = 20f;
    [Tooltip("The maximum distance to search for a valid NavMesh point from our ground hit.")]
    [SerializeField] private float maxNavMeshDistance = 2f;

    [Header("Testing/Auto-Spawn")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private float spawnInterval = 3f;
    private float nextSpawnTime;

    private void Update()
    {
        if (autoSpawn && Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No enemy prefabs assigned!");
            return;
        }

        Vector3 randomPoint = GetRandomPointInArea();
        
        // Step 1: Raycast down from ABOVE the spawner to find the physical ground
        Vector3 origin = new Vector3(randomPoint.x, transform.position.y + raycastHeight, randomPoint.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2))
        {
            // Step 2: Validate the hit point against the NavMesh
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, maxNavMeshDistance, NavMesh.AllAreas))
            {
                // navHit.position is guaranteed to be a valid spot on the NavMesh
                GameObject prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                Instantiate(prefabToSpawn, navHit.position, Quaternion.identity);
                return;
            }
        }
        
        Debug.LogWarning("EnemySpawner: Failed to find a valid NavMesh spawn position.");
    }

    private Vector3 GetRandomPointInArea()
    {
        float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomZ = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        return new Vector3(transform.position.x + randomX, 0, transform.position.z + randomZ);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        // Draw a wire cube representing the 2D spawn area on the XZ plane
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0, spawnAreaSize.y));
    }
}
