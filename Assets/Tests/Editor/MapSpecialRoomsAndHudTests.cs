using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class MapSpecialRoomsAndHudTests
{
    private const string MapNodePrefabPath = "Assets/_Game/Prefabs/Map/MapNode.prefab";

    private readonly List<Object> spawned = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object obj in spawned)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }
        spawned.Clear();
    }

    [Test]
    public void MapNode_prefab_uses_prefab_children_and_creates_no_runtime_objects_on_init()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapNodePrefabPath);
        Assert.That(prefab, Is.Not.Null, $"Missing {MapNodePrefabPath}");

        GameObject instance = Object.Instantiate(prefab);
        spawned.Add(instance);

        Node node = instance.GetComponent<Node>();
        Assert.That(node, Is.Not.Null);
        Assert.That(node.Ring, Is.Not.Null);
        Assert.That(node.Fill, Is.Not.Null);
        Assert.That(node.Icon, Is.Not.Null);
        Assert.That(node.Label, Is.Not.Null);

        int childrenBefore = instance.transform.childCount;
        node.Init(2, 1, NodeTypeEnum.Elite, _ => { }, _ => { });

        Assert.That(instance.transform.childCount, Is.EqualTo(childrenBefore));
        Assert.That(node.Icon.text, Is.EqualTo(Node.IconFor(NodeTypeEnum.Elite)));
    }

    [Test]
    public void MapHudView_formats_all_node_types_stats_and_legend()
    {
        Assert.That(MapHudView.FormatHeader(1, 2, 3), Does.Contain("ACT 1").And.Contain("SCENE 2").And.Contain("Take 3"));
        Assert.That(MapHudView.FormatStats(0f, 0f, 12), Does.Contain("-- / --").And.Contain("Buttons 12"));
        Assert.That(MapHudView.FormatStats(150f, 200f, 25), Does.Contain("150 / 200").And.Contain("Buttons 25"));

        Assert.That(MapHudView.FormatNodeDescription(0, NodeTypeEnum.Combat), Does.Contain("First Spot"));
        foreach (NodeTypeEnum type in Enum.GetValues(typeof(NodeTypeEnum)))
        {
            Assert.That(MapHudView.FormatNodeDescription(2, type), Is.Not.Empty);
        }

        string legend = MapHudView.FormatLegend();
        Assert.That(legend, Does.Contain("Fight").And.Contain("Health").And.Contain("Shop")
            .And.Contain("Treasure").And.Contain("Elite").And.Contain("Mystery").And.Contain("Final Fight"));
    }

    [Test]
    public void SpecialRoom_rest_and_health_heal_once_and_mark_prop_spent()
    {
        GameObject playerObj = new GameObject("TestPlayer");
        spawned.Add(playerObj);
        Health health = playerObj.AddComponent<Health>();
        health.maxHealth = 200f;
        health.currentHealth = 100f;

        GameObject room = SpecialRoomController.CreateRuntimeRoom(
            SpecialRoomKind.Health,
            new RoomRewards(currency: 0, healthDropChance: 1f, healAmount: 60f));
        spawned.Add(room);

        SpecialRoomController controller = room.GetComponent<SpecialRoomController>();
        Assert.That(controller, Is.Not.Null);
        Assert.That(controller.Interactable, Is.Not.Null);
        Assert.That(controller.Interactable.IsUsed, Is.False);

        bool firstUse = controller.Interactable.TryInteract(health);
        bool secondUse = controller.Interactable.TryInteract(health);

        Assert.That(firstUse, Is.True);
        Assert.That(secondUse, Is.False);
        Assert.That(controller.Interactable.IsUsed, Is.True);
        Assert.That(health.currentHealth, Is.EqualTo(160f).Within(0.01f));
    }

    [Test]
    public void SpecialRoom_deal_costs_non_lethal_percentage_and_fires_hook()
    {
        Assert.That(SpecialRoomInteractable.ComputeDealHealthCost(100f, 0.2f), Is.EqualTo(20f));
        Assert.That(SpecialRoomInteractable.ComputeDealHealthCost(1f, 0.2f), Is.EqualTo(0f));

        GameObject playerObj = new GameObject("TestPlayer");
        spawned.Add(playerObj);
        Health health = playerObj.AddComponent<Health>();
        health.maxHealth = 200f;
        health.currentHealth = 100f;

        GameObject room = SpecialRoomController.CreateRuntimeRoom(SpecialRoomKind.Deal);
        spawned.Add(room);

        bool dealFired = false;
        Action<SpecialRoomInteractable> handler = _ => dealFired = true;
        SpecialRoomInteractable.OnDealAccepted += handler;
        try
        {
            bool interacted = room.GetComponent<SpecialRoomController>().Interactable.TryInteract(health);
            Assert.That(interacted, Is.True);
            Assert.That(dealFired, Is.True);
            Assert.That(health.currentHealth, Is.EqualTo(80f).Within(0.01f));
        }
        finally
        {
            SpecialRoomInteractable.OnDealAccepted -= handler;
        }
    }

    [Test]
    public void SpecialRoom_narrative_fires_stub_hook_and_planner_sets_special_room_kinds()
    {
        GameObject room = SpecialRoomController.CreateRuntimeRoom(SpecialRoomKind.Narrative);
        spawned.Add(room);

        bool narrativeFired = false;
        Action<SpecialRoomInteractable> handler = _ => narrativeFired = true;
        SpecialRoomInteractable.OnNarrativeTriggered += handler;
        try
        {
            Assert.That(room.GetComponent<SpecialRoomController>().Interactable.TryInteract(null), Is.True);
            Assert.That(narrativeFired, Is.True);
        }
        finally
        {
            SpecialRoomInteractable.OnNarrativeTriggered -= handler;
        }

        var planner = new MapRoomPlanner(new MapRoomSettings(), new System.Random(42), eliteFirstRow: 3);
        List<PlannedRoom> healthPlan = planner.Plan(NodeTypeEnum.Health, 2, 0, false);
        List<PlannedRoom> treasurePlan = planner.Plan(NodeTypeEnum.Treasure, 3, 0, false);

        Assert.That(healthPlan[0].SpecialRoom, Is.EqualTo(SpecialRoomKind.Health));
        Assert.That(healthPlan[0].IsCalmSpecialRoom, Is.True);
        Assert.That(treasurePlan[0].SpecialRoom, Is.EqualTo(SpecialRoomKind.Treasure));
        Assert.That(treasurePlan[0].IsCalmSpecialRoom, Is.True);
    }
}
