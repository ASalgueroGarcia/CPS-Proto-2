using UnityEngine;

/// <summary>
/// An enemy that makes other enemies (Deck of Cards). WaveManager listens so the room stays
/// open until the spawner and everything it made are dead, and so spawned enemies drop nothing.
/// Lives in Core because Enemies and Waves both need it and neither references the other.
/// </summary>
public interface IEnemySpawner
{
    /// <summary>
    /// Raise with the new enemy right after instantiating it (it needs a Health). Never raise it
    /// once the spawner has died: a soldier made after the room cleared would not be tracked.
    /// </summary>
    event System.Action<GameObject> OnEnemySpawned;
}
