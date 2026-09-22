using UnityEngine;

public class PowerUpSpecialDamageConcrete : PowerUpConcreteBase
{
    [SerializeField] private PlayerStatsManager playerStats;

    private void Awake()
    {
        if (playerStats == null) {
            playerStats = PlayerStatsManager.Instance;
        }
    }

    protected override void Apply(float damageToAdd)
    {
        playerStats.MoreSpecialDamage(damageToAdd);
    }
}