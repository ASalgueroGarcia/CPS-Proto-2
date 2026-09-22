using UnityEngine;

public class PowerUpStunConcrete : PowerUpConcreteBase
{
    [SerializeField] private PlayerStatsManager playerStats;

    private void Awake()
    {
        if (playerStats == null) {
            playerStats = PlayerStatsManager.Instance;
        }
    }

    protected override void Apply(float stun)
    {
        playerStats.ApplyStun(stun);
    }
}