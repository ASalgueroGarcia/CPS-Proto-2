using UnityEngine;

public class PowerUpDashSpeedConcrete : PowerUpConcreteBase
{
    [SerializeField] private PlayerStatsManager playerStats;

    private void Awake()
    {
        if (playerStats == null) {
            playerStats = PlayerStatsManager.Instance;
        }
    }

    protected override void Apply(float dashSpeedToAdd)
    {
        playerStats.MoreDashSpeed(dashSpeedToAdd);
    }
}