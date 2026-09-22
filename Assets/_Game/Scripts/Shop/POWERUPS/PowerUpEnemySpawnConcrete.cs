using UnityEngine;

public class PowerUpEnemySpawnConcrete : PowerUpConcreteBase
{
    [SerializeField] private PlayerStatsManager playerStats;
    private void Awake()
    {
        if (playerStats == null) {
            playerStats = PlayerStatsManager.Instance;
        }
    }

    protected override void Apply(float SpawnRateToAdd)
    {
        playerStats.MoreEnemySpawnRate(SpawnRateToAdd);
    }
}