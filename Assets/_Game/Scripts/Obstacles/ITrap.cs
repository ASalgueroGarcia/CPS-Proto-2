using UnityEngine;

// INTERFACE FOR THE OBSTACULES & ssssTRAPS.
public interface ITrap
{
    void TrapActive();
    void TrapDesactive();
    void OnPlayerEnter(GameObject player);
    void OnEnemyEnter(GameObject enemy);
}
