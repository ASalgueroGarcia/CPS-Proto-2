using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>Which rooms a map node plays (GDD > Level Design > Fight Nodes & Rooms).</summary>
public class MapRoomPlannerTests
{
    private const int Seeds = 300;

    private static MapRoomPlanner Planner(int seed = 1, MapRoomSettings settings = null, int? eliteFirstRow = null) =>
        new MapRoomPlanner(settings ?? new MapRoomSettings(), new Random(seed), eliteFirstRow);

    private static bool IsFight(RoomType room) => room != RoomType.Shop && room != RoomType.Treasure;

    [Test]
    public void Shop_health_treasure_elite_mystery_and_final_fight_are_one_room()
    {
        Assert.AreEqual(new[] { RoomType.Shop }, Planner().Plan(NodeTypeEnum.Shop, 3, 0, false).Select(r => r.Room));
        Assert.AreEqual(new[] { RoomType.Treasure }, Planner().Plan(NodeTypeEnum.Health, 3, 0, false).Select(r => r.Room));
        Assert.AreEqual(new[] { RoomType.Treasure }, Planner().Plan(NodeTypeEnum.Treasure, 3, 0, false).Select(r => r.Room));
        Assert.AreEqual(new[] { RoomType.Hard }, Planner().Plan(NodeTypeEnum.Elite, 4, 0, false).Select(r => r.Room));
        Assert.AreEqual(1, Planner().Plan(NodeTypeEnum.Mystery, 4, 0, false).Count);
        Assert.AreEqual(new[] { RoomType.MiniBoss }, Planner().Plan(NodeTypeEnum.FinalFight, 7, 0, false).Select(r => r.Room));
    }

    [Test]
    public void Health_node_is_calm_and_heals_60_hp()
    {
        var settings = new MapRoomSettings();
        PlannedRoom room = Planner(1, settings).Plan(NodeTypeEnum.Health, 2, 0, false).Single();

        Assert.AreEqual(RoomType.Treasure, room.Room);
        Assert.IsNull(room.Encounter);
        Assert.IsTrue(room.Rewards.HasValue);
        Assert.AreEqual(0, room.Rewards.Value.Currency);
        Assert.AreEqual(1f, room.Rewards.Value.HealthDropChance);
        Assert.AreEqual(60f, room.Rewards.Value.HealAmount);
    }

    [Test]
    public void Elite_node_plans_one_elite_combat_room_for_hl3()
    {
        PlannedRoom elite = Planner(1).Plan(NodeTypeEnum.Elite, 4, 1, false).Single();

        Assert.AreEqual(RoomType.Hard, elite.Room);
        Assert.IsTrue(elite.IsElite);
        Assert.IsNull(elite.Rewards);
        Assert.IsTrue(elite.Encounter.HasValue);
        Assert.AreEqual(2, elite.Encounter.Value.scene);
        Assert.AreEqual(EncounterNodeKind.Elite, elite.Encounter.Value.nodeKind);
        Assert.IsTrue(elite.Encounter.Value.IsElite);
        Assert.IsFalse(elite.Encounter.Value.isFirstSpot);
        Assert.AreEqual(0, elite.Encounter.Value.roomInNode);
    }

    [Test]
    public void Final_fight_is_the_boss_on_the_last_scene()
    {
        Assert.AreEqual(RoomType.Boss, Planner().Plan(NodeTypeEnum.FinalFight, 7, 2, true)[0].Room);
    }

    [Test]
    public void Fight_nodes_are_four_or_five_rooms()
    {
        for (int seed = 0; seed < Seeds; seed++)
        {
            Assert.That(Planner(seed).Plan(NodeTypeEnum.Combat, 4, 0, false).Count, Is.InRange(4, 5));
        }
    }

    [Test]
    public void First_node_is_all_entrance_fights_with_no_special_rooms()
    {
        for (int seed = 0; seed < Seeds; seed++)
        {
            List<PlannedRoom> rooms = Planner(seed).Plan(NodeTypeEnum.Combat, 0, 0, false);
            Assert.IsTrue(rooms.All(r => r.Room == RoomType.Entrance));
        }
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
        for (int seed = 0; seed < Seeds; seed++)
        {
            List<PlannedRoom> rooms = Planner(seed).Plan(NodeTypeEnum.Combat, 4, 0, false);
            Assert.IsTrue(IsFight(rooms[0].Room));
            Assert.IsTrue(IsFight(rooms[rooms.Count - 1].Room));
            Assert.LessOrEqual(rooms.Count(r => !IsFight(r.Room)), 1);
        }
    }

    [Test]
    public void Fight_nodes_roll_25_percent_rest_and_15_percent_shop_when_uncapped()
    {
        var settings = new MapRoomSettings { maxShopsPerScene = 10000 };
        MapRoomPlanner planner = Planner(17, settings);

        const int trials = 4000;
        int restNodes = 0;
        int shopNodes = 0;
        for (int i = 0; i < trials; i++)
        {
            List<PlannedRoom> rooms = planner.Plan(NodeTypeEnum.Combat, 4, 0, false);
            if (rooms.Any(r => r.Room == RoomType.Treasure)) restNodes++;
            if (rooms.Any(r => r.Room == RoomType.Shop)) shopNodes++;
        }

        Assert.AreEqual(0.25f, restNodes / (float)trials, 0.03f, "Fight-node Rest chance should be ~25%.");
        Assert.AreEqual(0.15f, shopNodes / (float)trials, 0.03f, "Fight-node Shop chance should be ~15%.");
    }

    [Test]
    public void Shop_rooms_inside_fight_nodes_stay_under_the_cap_and_shop_node_does_not_count()
    {
        var alwaysShop = new MapRoomSettings { restRoomChance = 0f, shopRoomChance = 1f };
        for (int seed = 0; seed < Seeds; seed++)
        {
            MapRoomPlanner planner = Planner(seed, alwaysShop);

            Assert.AreEqual(1, planner.Plan(NodeTypeEnum.Shop, 3, 0, false).Count(r => r.Room == RoomType.Shop));

            int fightNodeShops = 0;
            for (int row = 1; row < 7; row++)
            {
                fightNodeShops += planner.Plan(NodeTypeEnum.Combat, row, 0, false).Count(r => r.Room == RoomType.Shop);
            }

            Assert.AreEqual(alwaysShop.maxShopsPerScene, fightNodeShops);
            Assert.AreEqual(1, planner.Plan(NodeTypeEnum.Shop, 4, 0, false).Count(r => r.Room == RoomType.Shop));
        }
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
    public void Combat_and_final_fight_rooms_carry_encounter_context_without_reward_overrides()
    {
        var settings = new MapRoomSettings { restRoomChance = 0f, shopRoomChance = 0f };

        List<PlannedRoom> combatRooms = Planner(1, settings).Plan(NodeTypeEnum.Combat, 4, 1, false);
        for (int i = 0; i < combatRooms.Count; i++)
        {
            Assert.IsNull(combatRooms[i].Rewards);
            Assert.IsTrue(combatRooms[i].Encounter.HasValue);
            Assert.AreEqual(2, combatRooms[i].Encounter.Value.scene);
            Assert.AreEqual(EncounterNodeKind.Fight, combatRooms[i].Encounter.Value.nodeKind);
            Assert.IsFalse(combatRooms[i].Encounter.Value.isFirstSpot);
            Assert.AreEqual(i, combatRooms[i].Encounter.Value.roomInNode);
        }

        foreach (PlannedRoom room in Planner(1, settings).Plan(NodeTypeEnum.Combat, 0, 0, false))
        {
            Assert.IsNull(room.Rewards);
            Assert.AreEqual(EncounterNodeKind.Fight, room.Encounter.Value.nodeKind);
            Assert.IsTrue(room.Encounter.Value.isFirstSpot);
        }

        PlannedRoom finalFight = Planner(1, settings).Plan(NodeTypeEnum.FinalFight, 7, 2, true)[0];
        Assert.IsNull(finalFight.Rewards);
        Assert.AreEqual(3, finalFight.Encounter.Value.scene);
        Assert.AreEqual(EncounterNodeKind.FinalFight, finalFight.Encounter.Value.nodeKind);
    }

    [Test]
    public void Treasure_pays_buttons_by_scene_or_heals()
    {
        var buttonsOnly = new MapRoomSettings { treasureHealChance = 0f };
        Assert.AreEqual(10, Planner(1, buttonsOnly).Plan(NodeTypeEnum.Treasure, 3, 0, false)[0].Rewards.Value.Currency);
        Assert.AreEqual(12, Planner(1, buttonsOnly).Plan(NodeTypeEnum.Treasure, 3, 1, false)[0].Rewards.Value.Currency);
        Assert.AreEqual(14, Planner(1, buttonsOnly).Plan(NodeTypeEnum.Treasure, 3, 2, true)[0].Rewards.Value.Currency);
        Assert.AreEqual(14, Planner(1, buttonsOnly).Plan(NodeTypeEnum.Treasure, 3, 9, true)[0].Rewards.Value.Currency);

        var healOnly = new MapRoomSettings { treasureHealChance = 1f };
        Assert.AreEqual(healOnly.treasureHeal, Planner(1, healOnly).Plan(NodeTypeEnum.Treasure, 3, 0, false)[0].Rewards.Value.HealAmount);
    }

    [Test]
    public void Mystery_resolves_to_gdd_weights_and_never_spawns_shop()
    {
        var settings = new MapRoomSettings();
        var rng = new Random(99);
        var counts = Enum.GetValues(typeof(MysteryOutcome)).Cast<MysteryOutcome>().ToDictionary(o => o, _ => 0);

        const int trials = 5000;
        for (int i = 0; i < trials; i++)
        {
            MysteryOutcome outcome = MysteryResolver.Resolve(rng, settings, row: 4, playerHealthRatio: 1f);
            counts[outcome]++;
        }

        Assert.AreEqual(0.30f, counts[MysteryOutcome.Fight] / (float)trials, 0.03f);
        Assert.AreEqual(0.25f, counts[MysteryOutcome.Treasure] / (float)trials, 0.03f);
        Assert.AreEqual(0.25f, counts[MysteryOutcome.Deal] / (float)trials, 0.03f);
        Assert.AreEqual(0.10f, counts[MysteryOutcome.Elite] / (float)trials, 0.03f);
        Assert.AreEqual(0.10f, counts[MysteryOutcome.Narrative] / (float)trials, 0.03f);

        for (int seed = 0; seed < Seeds; seed++)
        {
            PlannedRoom room = Planner(seed, settings).Plan(NodeTypeEnum.Mystery, 4, 0, false, 1f).Single();
            Assert.AreNotEqual(RoomType.Shop, room.Room, "Mystery nodes never resolve to Shop.");
            Assert.IsTrue(room.Mystery.HasValue);
            if (room.Mystery == MysteryOutcome.Elite) Assert.IsTrue(room.IsElite);
        }
    }

    [Test]
    public void Mystery_safety_rule_excludes_elite_below_40_percent_hp_or_before_elite_row_and_redistributes_weights()
    {
        var settings = new MapRoomSettings();
        var rng = new Random(123);
        var lowHpCounts = Enum.GetValues(typeof(MysteryOutcome)).Cast<MysteryOutcome>().ToDictionary(o => o, _ => 0);

        const int trials = 4500;
        for (int i = 0; i < trials; i++)
        {
            MysteryOutcome outcome = MysteryResolver.Resolve(rng, settings, row: 5, playerHealthRatio: 0.39f);
            lowHpCounts[outcome]++;
        }

        Assert.AreEqual(0, lowHpCounts[MysteryOutcome.Elite], "Safety rule: never roll Elite below 40% HP.");
        Assert.AreEqual(30f / 90f, lowHpCounts[MysteryOutcome.Fight] / (float)trials, 0.03f);
        Assert.AreEqual(25f / 90f, lowHpCounts[MysteryOutcome.Treasure] / (float)trials, 0.03f);
        Assert.AreEqual(25f / 90f, lowHpCounts[MysteryOutcome.Deal] / (float)trials, 0.03f);
        Assert.AreEqual(10f / 90f, lowHpCounts[MysteryOutcome.Narrative] / (float)trials, 0.03f);

        // Early rows (row 1 and row 2 = GDD rows 2-3) also exclude Elite even at full HP.
        for (int seed = 0; seed < Seeds; seed++)
        {
            Assert.AreNotEqual(MysteryOutcome.Elite, Planner(seed, settings).ResolveMystery(row: 1, playerHealthRatio: 1f));
            Assert.AreNotEqual(MysteryOutcome.Elite, Planner(seed, settings).ResolveMystery(row: 2, playerHealthRatio: 1f));
            Assert.AreNotEqual(MysteryOutcome.Elite, Planner(seed, settings).Plan(NodeTypeEnum.Mystery, 4, 0, false, 0.25f)[0].Mystery);
        }

        // Exact 40% HP on eliteFirstRow (row 3) still allows Elite.
        int boundaryElites = Enumerable.Range(0, Seeds)
            .Count(seed => Planner(seed, settings).ResolveMystery(row: 3, playerHealthRatio: 0.40f) == MysteryOutcome.Elite);
        Assert.Greater(boundaryElites, 0, "At >= 40% HP on row 3, Elite remains eligible.");
    }

    [Test]
    public void Same_seed_plans_the_same_rooms()
    {
        var a = Planner(11).Plan(NodeTypeEnum.Combat, 5, 1, false).Select(r => r.Room);
        var b = Planner(11).Plan(NodeTypeEnum.Combat, 5, 1, false).Select(r => r.Room);
        Assert.AreEqual(a, b);
    }
}
