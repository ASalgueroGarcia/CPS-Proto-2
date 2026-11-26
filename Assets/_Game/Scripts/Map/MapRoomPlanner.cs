using System;
using System.Collections.Generic;

/// <summary>
/// Decides which rooms a map node plays. Plain C#: give it a seeded Random and it is repeatable.
/// Keep one planner per map so the shop-room limit counts across its fight nodes.
/// </summary>
public class MapRoomPlanner
{
    private readonly MapRoomSettings settings;
    private readonly Random rng;
    private readonly int? eliteFirstRow;
    private int shopVisits;

    public MapRoomPlanner(MapRoomSettings settings, Random rng, int? eliteFirstRow = null)
    {
        this.settings = settings;
        this.rng = rng;
        this.eliteFirstRow = eliteFirstRow;
    }

    /// <summary>Rooms for the node the player just entered, in play order.</summary>
    /// <param name="sceneIndex">Scene of the act, from 0.</param>
    /// <param name="lastScene">True on the act's last scene, where the final fight is the boss.</param>
    /// <param name="playerHealthRatio">Current HP / Max HP in [0, 1], used by Mystery safety rules.</param>
    public List<PlannedRoom> Plan(
        NodeTypeEnum type,
        int row,
        int sceneIndex,
        bool lastScene,
        float playerHealthRatio = 1f)
    {
        var rooms = new List<PlannedRoom>();
        int scene = sceneIndex + 1;
        switch (type)
        {
            case NodeTypeEnum.Shop:
                // GDD: the Shop node is 1 per map; maxShopsPerScene caps shop rooms inside fight nodes.
                rooms.Add(new PlannedRoom(RoomType.Shop));
                return rooms;
            case NodeTypeEnum.Health:
                rooms.Add(new PlannedRoom(
                    RoomType.Treasure,
                    new RoomRewards(currency: 0, healthDropChance: 1f, healAmount: settings.healthNodeHeal)));
                return rooms;
            case NodeTypeEnum.Treasure:
                rooms.Add(new PlannedRoom(RoomType.Treasure, TreasureRewards(sceneIndex)));
                return rooms;
            case NodeTypeEnum.Elite:
                rooms.Add(new PlannedRoom(
                    RoomType.Hard,
                    encounter: new EncounterContext(scene, EncounterNodeKind.Elite, false, 0)));
                return rooms;
            case NodeTypeEnum.Mystery:
                MysteryOutcome outcome = ResolveMystery(row, playerHealthRatio);
                rooms.Add(PlanMysteryRoom(outcome, row, sceneIndex, scene));
                return rooms;
            case NodeTypeEnum.FinalFight:
                rooms.Add(new PlannedRoom(
                    lastScene ? RoomType.Boss : RoomType.MiniBoss,
                    encounter: new EncounterContext(scene, EncounterNodeKind.FinalFight, false, 0)));
                return rooms;
        }

        // Combat node: 4-5 rooms driven by WaveManager's EncounterTable, with optional Rest or Shop room in the middle.
        bool firstSpot = row == 0;
        int count = rng.Next(settings.minRooms, settings.maxRooms + 1);
        RoomType tier = BaseTier(row);
        for (int i = 0; i < count; i++)
        {
            rooms.Add(new PlannedRoom(
                firstSpot ? tier : StepUp(tier, i / settings.roomsPerDifficultyStep),
                encounter: new EncounterContext(scene, EncounterNodeKind.Fight, firstSpot, i)));
        }

        // The first node never swaps a fight for a special room, and a swap needs a room that is neither first nor last.
        if (firstSpot || count < 3) return rooms;

        double roll = rng.NextDouble();
        int middle = rng.Next(1, count - 1);
        if (roll < settings.restRoomChance)
        {
            rooms[middle] = new PlannedRoom(
                RoomType.Treasure,
                new RoomRewards(currency: 0, healthDropChance: 1f, healAmount: settings.restRoomHeal));
        }
        else if (shopVisits < settings.maxShopsPerScene && roll < settings.restRoomChance + settings.shopRoomChance)
        {
            shopVisits++;
            rooms[middle] = new PlannedRoom(RoomType.Shop);
        }

        return rooms;
    }

    public MysteryOutcome ResolveMystery(int row, float playerHealthRatio = 1f)
    {
        return MysteryResolver.Resolve(rng, settings, row, playerHealthRatio, eliteFirstRow);
    }

    private PlannedRoom PlanMysteryRoom(MysteryOutcome outcome, int row, int sceneIndex, int scene)
    {
        return outcome switch
        {
            MysteryOutcome.Treasure => new PlannedRoom(RoomType.Treasure, rewards: TreasureRewards(sceneIndex), mystery: outcome),
            MysteryOutcome.Deal => new PlannedRoom(RoomType.Treasure, rewards: TreasureRewards(sceneIndex), mystery: outcome),
            MysteryOutcome.Elite => new PlannedRoom(
                RoomType.Hard,
                encounter: new EncounterContext(scene, EncounterNodeKind.Elite, false, 0),
                mystery: outcome),
            MysteryOutcome.Narrative => new PlannedRoom(
                RoomType.Treasure,
                rewards: new RoomRewards(currency: 0, healthDropChance: 0f),
                mystery: outcome),
            _ => new PlannedRoom(
                BaseTier(row),
                encounter: new EncounterContext(scene, EncounterNodeKind.Fight, false, 0),
                mystery: MysteryOutcome.Fight)
        };
    }

    private RoomRewards TreasureRewards(int sceneIndex)
    {
        if (rng.NextDouble() < settings.treasureHealChance)
            return new RoomRewards(currency: 0, healthDropChance: 1f, healAmount: settings.treasureHeal);

        int[] buttons = settings.treasureButtons;
        return new RoomRewards(currency: buttons[Math.Max(0, Math.Min(sceneIndex, buttons.Length - 1))], healthDropChance: 0f);
    }

    private RoomType BaseTier(int row)
    {
        RoomType[] tiers = settings.rowTiers;
        return tiers[Math.Max(0, Math.Min(row, tiers.Length - 1))];
    }

    private static RoomType StepUp(RoomType tier, int steps)
    {
        return (RoomType)Math.Min((int)tier + steps, (int)RoomType.Hard);
    }
}
