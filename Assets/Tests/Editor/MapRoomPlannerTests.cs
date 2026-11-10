using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>Which rooms a map node plays (GDD > Level Design > Fight Nodes & Rooms).</summary>
public class MapRoomPlannerTests
{
    private static MapRoomPlanner Planner(int seed = 1, MapRoomSettings settings = null) =>
        new MapRoomPlanner(settings ?? new MapRoomSettings(), new System.Random(seed));

    private static bool IsFight(RoomType room) => room != RoomType.Shop && room != RoomType.Treasure;

    [Test]
    public void Shop_treasure_and_final_fight_are_one_room()
    {
        Assert.AreEqual(new[] { RoomType.Shop }, Planner().Plan(NodeTypeEnum.Shop, 3, 0, false).Select(r => r.Room));
        Assert.AreEqual(new[] { RoomType.Treasure }, Planner().Plan(NodeTypeEnum.Treasure, 3, 0, false).Select(r => r.Room));
        Assert.AreEqual(new[] { RoomType.MiniBoss }, Planner().Plan(NodeTypeEnum.FinalFight, 7, 0, false).Select(r => r.Room));
    }

    [Test]
    public void Final_fight_is_the_boss_on_the_last_scene()
    {
        Assert.AreEqual(RoomType.Boss, Planner().Plan(NodeTypeEnum.FinalFight, 7, 2, true)[0].Room);
    }

    [Test]
    public void Fight_nodes_are_four_or_five_rooms()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            foreach (var type in new[] { NodeTypeEnum.Combat, NodeTypeEnum.MoreHealth, NodeTypeEnum.MoreButtons })
                Assert.That(Planner(seed).Plan(type, 4, 0, false).Count, Is.InRange(4, 5));
        }
    }

    [Test]
    public void First_node_is_all_entrance_fights()
    {
        for (int seed = 0; seed < 200; seed++)
            Assert.IsTrue(Planner(seed).Plan(NodeTypeEnum.Combat, 0, 0, false).All(r => r.Room == RoomType.Entrance));
    }

    [Test]
    public void Fights_get_harder_every_two_rooms_and_stop_at_hard()
    {
        var noSpecials = new MapRoomSettings { restRoomChance = 0f, shopRoomChance = 0f, minRooms = 5, maxRooms = 5 };

        Assert.AreEqual(
            new[] { RoomType.Medium, RoomType.Medium, RoomType.MediumHard, RoomType.MediumHard, RoomType.Hard },
            Planner(1, noSpecials).Plan(NodeTypeEnum.Combat, 3, 0, false).Select(r => r.Room));
        Assert.AreEqual(
            new[] { RoomType.MediumHard, RoomType.MediumHard, RoomType.Hard, RoomType.Hard, RoomType.Hard },
            Planner(1, noSpecials).Plan(NodeTypeEnum.Combat, 6, 0, false).Select(r => r.Room));
    }

    [Test]
    public void Special_rooms_never_open_or_close_a_node()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            List<PlannedRoom> rooms = Planner(seed).Plan(NodeTypeEnum.Combat, 4, 0, false);
            Assert.IsTrue(IsFight(rooms[0].Room));
            Assert.IsTrue(IsFight(rooms[rooms.Count - 1].Room));
            Assert.LessOrEqual(rooms.Count(r => !IsFight(r.Room)), 1);
        }
    }

    [Test]
    public void Shop_visits_stay_under_the_cap_across_a_map()
    {
        var alwaysShop = new MapRoomSettings { restRoomChance = 0f, shopRoomChance = 1f };
        MapRoomPlanner planner = Planner(3, alwaysShop);

        int shops = planner.Plan(NodeTypeEnum.Shop, 3, 0, false).Count(r => r.Room == RoomType.Shop);
        for (int row = 1; row < 7; row++)
            shops += planner.Plan(NodeTypeEnum.Combat, row, 0, false).Count(r => r.Room == RoomType.Shop);

        Assert.AreEqual(alwaysShop.maxShopsPerScene, shops);
    }

    [Test]
    public void Rest_room_heals_and_pays_nothing()
    {
        var alwaysRest = new MapRoomSettings { restRoomChance = 1f };
        PlannedRoom rest = Planner(1, alwaysRest).Plan(NodeTypeEnum.Combat, 4, 0, false).Single(r => r.Room == RoomType.Treasure);

        Assert.AreEqual(alwaysRest.restRoomHeal, rest.Rewards.Value.HealAmount);
        Assert.AreEqual(1f, rest.Rewards.Value.HealthDropChance);
        Assert.AreEqual(0, rest.Rewards.Value.Currency);
    }

    [Test]
    public void More_health_and_more_buttons_change_the_fight_rewards()
    {
        var settings = new MapRoomSettings { restRoomChance = 0f, shopRoomChance = 0f };

        foreach (PlannedRoom room in Planner(1, settings).Plan(NodeTypeEnum.MoreHealth, 4, 0, false))
            Assert.AreEqual(settings.moreHealthDropChance, room.Rewards.Value.HealthDropChance);
        foreach (PlannedRoom room in Planner(1, settings).Plan(NodeTypeEnum.MoreButtons, 4, 0, false))
            Assert.AreEqual(settings.moreButtonsMultiplier, room.Rewards.Value.CurrencyMultiplier);
        foreach (PlannedRoom room in Planner(1, settings).Plan(NodeTypeEnum.Combat, 4, 0, false))
            Assert.IsNull(room.Rewards);
    }

    [Test]
    public void Treasure_pays_buttons_by_scene_or_heals()
    {
        var buttonsOnly = new MapRoomSettings { treasureHealChance = 0f };
        Assert.AreEqual(10, Planner(1, buttonsOnly).Plan(NodeTypeEnum.Treasure, 3, 0, false)[0].Rewards.Value.Currency);
        Assert.AreEqual(14, Planner(1, buttonsOnly).Plan(NodeTypeEnum.Treasure, 3, 2, true)[0].Rewards.Value.Currency);
        Assert.AreEqual(14, Planner(1, buttonsOnly).Plan(NodeTypeEnum.Treasure, 3, 9, true)[0].Rewards.Value.Currency);

        var healOnly = new MapRoomSettings { treasureHealChance = 1f };
        Assert.AreEqual(healOnly.treasureHeal, Planner(1, healOnly).Plan(NodeTypeEnum.Treasure, 3, 0, false)[0].Rewards.Value.HealAmount);
    }

    [Test]
    public void Same_seed_plans_the_same_rooms()
    {
        var a = Planner(11).Plan(NodeTypeEnum.Combat, 5, 1, false).Select(r => r.Room);
        var b = Planner(11).Plan(NodeTypeEnum.Combat, 5, 1, false).Select(r => r.Room);
        Assert.AreEqual(a, b);
    }
}
