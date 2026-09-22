using UnityEngine;

public class PowerUpCriticalDamageConcrete : PowerUpConcreteBase
{
    [SerializeField] private PlayerStatsManager playerStats;

    private void Awake()
    {
        if (playerStats == null) {
            playerStats = PlayerStatsManager.Instance;
        }
    }

    protected override void Apply(float criticalDamageToAdd)
    {
        playerStats.MoreCriticalDamage(criticalDamageToAdd);
    }
}