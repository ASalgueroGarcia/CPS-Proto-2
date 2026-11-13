using System;
using System.Collections.Generic;

/// <summary>
/// Decides which rooms a map node plays. Plain C#: give it a seeded Random and it is repeatable.
/// Keep one planner per map so the shop limit counts across its nodes.
/// </summary>
public class MapRoomPlanner
{
    private readonly MapRoomSettings settings;
    private readonly Random rng;
    private int shopVisits;

    public MapRoomPlanner(MapRoomSettings settings, Random rng)
    {
        this.settings = settings;
        this.rng = rng;
    }

    /// <summary>Rooms for the node the player just entered, in play order.</summary>
    /// <param name="sceneIndex">Scene of the act, from 0.</param>
    /// <param name="lastScene">True on the act's last scene, where the final fight is the boss.</param>
    public List<PlannedRoom> Plan(NodeTypeEnum type, int row, int sceneIndex, bool lastScene)
    {
        var rooms = new List<PlannedRoom>();
        switch (type)
        {
            case NodeTypeEnum.Shop:
                shopVisits++;
                rooms.Add(new PlannedRoom(RoomType.Shop));
                return rooms;
            case NodeTypeEnum.Treasure:
                rooms.Add(new PlannedRoom(RoomType.Treasure, TreasureRewards(sceneIndex)));
                return rooms;
            case NodeTypeEnum.FinalFight:
                rooms.Add(new PlannedRoom(lastScene ? RoomType.Boss : RoomType.MiniBoss));
                return rooms;
        }

        RoomRewards? rewards = type switch
        {
            NodeTypeEnum.MoreHealth => new RoomRewards(healthDropChance: settings.moreHealthDropChance),
            NodeTypeEnum.MoreButtons => new RoomRewards(currencyMultiplier: settings.moreButtonsMultiplier),
            _ => (RoomRewards?)null
        };

        int count = rng.Next(settings.minRooms, settings.maxRooms + 1);
        RoomType tier = BaseTier(row);
        for (int i = 0; i < count; i++)
        {
            rooms.Add(new PlannedRoom(row == 0 ? tier : StepUp(tier, i / settings.roomsPerDifficultyStep), rewards));
        }

        // The first node never swaps a fight for a special room, and a swap needs a room that is neither first nor last.
        if (row == 0 || count < 3) return rooms;

        double roll = rng.NextDouble();
        int middle = rng.Next(1, count - 1);
        if (roll < settings.restRoomChance)
        {
            rooms[middle] = new PlannedRoom(RoomType.Treasure, new RoomRewards(currency: 0, healthDropChance: 1f, healAmount: settings.restRoomHeal));
        }
        else if (shopVisits < settings.maxShopsPerScene && roll < settings.restRoomChance + settings.shopRoomChance)
        {
            shopVisits++;
            rooms[middle] = new PlannedRoom(RoomType.Shop);
        }

        return rooms;
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