using UnityEngine;

public class PowerUpHealthConcrete : PowerUpConcreteBase
{
    [SerializeField] private PlayerStatsManager playerStats;

    private void Awake()
    {
        if (playerStats == null) {
            playerStats = PlayerStatsManager.Instance;
        }
    }

    protected override void Apply(float healthToIncrease)
    {
        playerStats.MoreMaxHealth(healthToIncrease);
    }
}
