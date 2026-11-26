using UnityEngine;

/// <summary>
/// Test stand-in for a Deck of Cards: makes one soldier per Spawn() call.
/// Outside Editor/ because Unity can't attach a MonoBehaviour from an editor assembly.
/// </summary>
public class StubEnemySpawner : MonoBehaviour, IEnemySpawner
{
    public event System.Action<GameObject> OnEnemySpawned;

    /// <summary>Soldier template (needs a Health). Instantiated like a real Deck would, so its events are set up.</summary>
    public GameObject soldierPrefab;

    public GameObject Spawn()
    {
        GameObject soldier = Instantiate(soldierPrefab);
        OnEnemySpawned?.Invoke(soldier);
        return soldier;
    }
}
