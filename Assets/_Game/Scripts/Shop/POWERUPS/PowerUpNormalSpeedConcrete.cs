using UnityEngine;

public class PowerUpNormalSpeedConcrete : PowerUpConcreteBase
{
    [SerializeField] private PlayerStatsManager playerStats;

    private void Awake()
    {
        if (playerStats == null) {
            playerStats = PlayerStatsManager.Instance;
        }
    }

    protected override void Apply(float speedToAdd)
    {
        playerStats.MoreSpeed(speedToAdd);
    }
}