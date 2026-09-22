using UnityEngine;

public class PowerUpEnemyLootConcrete : PowerUpConcreteBase
{
    [SerializeField] private PlayerStatsManager playerStats;
    private void Awake()
    {
        if (playerStats == null) {
            playerStats = PlayerStatsManager.Instance;
        }
    }

    protected override void Apply(float loot)
    {
        playerStats.MoreEnemyLootRate(loot);
    }
}
