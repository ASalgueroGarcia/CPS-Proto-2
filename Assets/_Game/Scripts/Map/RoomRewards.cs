/// <summary>Reward overrides for one room. The defaults leave the room's own rewards untouched.</summary>
public readonly struct RoomRewards
{
    public readonly float CurrencyMultiplier;
    public readonly int Currency;
    public readonly float HealthDropChance;
    public readonly float HealAmount;

    public RoomRewards(float currencyMultiplier = 1f, int currency = -1, float healthDropChance = -1f, float healAmount = -1f)
    {
        CurrencyMultiplier = currencyMultiplier;
        Currency = currency;
        HealthDropChance = healthDropChance;
        HealAmount = healAmount;
    }

    public void Apply(WaveManager waves) => waves.SetRewards(CurrencyMultiplier, Currency, HealthDropChance, HealAmount);
}