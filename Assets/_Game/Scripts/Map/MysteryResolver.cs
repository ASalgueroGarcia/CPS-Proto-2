using System;

/// <summary>
/// Resolves a Mystery (?) map node on entry using the GDD outcome weights.
/// Never rolls Shop; excludes Elite (redistributing its weight) below the HP safety threshold or before eliteFirstRow.
/// </summary>
public static class MysteryResolver
{
    public static MysteryOutcome Resolve(
        Random rng,
        MapRoomSettings settings,
        int row,
        float playerHealthRatio = 1f,
        int? eliteFirstRowOverride = null)
    {
        int eliteFirstRow = eliteFirstRowOverride ?? settings.mysteryEliteFirstRow;
        bool allowElite = row >= eliteFirstRow && playerHealthRatio >= settings.mysteryEliteMinHealthRatio;

        int fight = Math.Max(0, settings.mysteryFightWeight);
        int treasure = Math.Max(0, settings.mysteryTreasureWeight);
        int deal = Math.Max(0, settings.mysteryDealWeight);
        int elite = allowElite ? Math.Max(0, settings.mysteryEliteWeight) : 0;
        int narrative = Math.Max(0, settings.mysteryNarrativeWeight);

        int total = fight + treasure + deal + elite + narrative;
        if (total <= 0) return MysteryOutcome.Fight;

        int roll = rng.Next(total);
        if (roll < fight) return MysteryOutcome.Fight;
        roll -= fight;
        if (roll < treasure) return MysteryOutcome.Treasure;
        roll -= treasure;
        if (roll < deal) return MysteryOutcome.Deal;
        roll -= deal;
        if (roll < elite) return MysteryOutcome.Elite;
        return MysteryOutcome.Narrative;
    }
}
