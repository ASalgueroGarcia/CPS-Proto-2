using UnityEngine;

public abstract class TrapBase : MonoBehaviour, ITrap
{
    public float TrapRadius;
    public float damageToEnemy;
    public float damageToPlayer;
    public abstract void TrapActive();
    public abstract void TrapDesactive();
    public abstract void OnPlayerEnter(GameObject player);
    public abstract void OnEnemyEnter(GameObject enemy);
}