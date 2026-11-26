using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

/// <summary>
/// MapGenerator against the live GDD Scene Map rules (Level Design > In-Run > "In-Run in Depth" > Scene Map),
/// over 300+ seeds. Rows here are 0-based: GDD row 1 = row 0, row 8 = MapGrid.LastRow.
/// </summary>
public class MapGeneratorTests
{
    private const int Seeds = 300;

    private static IEnumerable<MapGrid> Maps(MapGenerationSettings settings = null)
    {
        for (int seed = 0; seed < Seeds; seed++)
        {
            yield return settings == null
                ? MapGenerator.Generate(new Random(seed))
                : MapGenerator.Generate(new Random(seed), settings);
        }
    }

    [Test]
    public void Grid_Is8RowsBy4Columns()
    {
        Assert.AreEqual(8, MapGrid.Rows);
        Assert.AreEqual(4, MapGrid.Columns);
    }

    [Test]
    public void FourPaths_LeaveTheFirstSpot_IntoTwoToFourColumns()
    {
        foreach (MapGrid map in Maps())
        {
            Assert.That(map.Children(0, 0).Count, Is.InRange(2, 4));
        }
    }

    [Test]
    public void FirstAndLastRows_AreSingleFixedNodes()
    {
        foreach (MapGrid map in Maps())
        {
            CollectionAssert.AreEqual(new[] { 0 }, map.NodesInRow(0), "Row 1 is the First Spot only.");
            CollectionAssert.AreEqual(new[] { 0 }, map.NodesInRow(MapGrid.LastRow), "Row 8 is the Final Fight only.");
            Assert.AreEqual(NodeTypeEnum.Combat, map.TypeAt(0, 0));
            Assert.AreEqual(NodeTypeEnum.FinalFight, map.TypeAt(MapGrid.LastRow, 0));
        }
    }

    [Test]
    public void SecondRow_HasAtLeastTwoNodes()
    {
        // The first two paths never start in the same column.
        foreach (MapGrid map in Maps())
        {
            int count = map.NodesInRow(1).Count;
            Assert.That(count, Is.InRange(2, MapGrid.Columns));
        }
    }

    [Test]
    public void EveryNode_IsOnAPathFromStartToEnd()
    {
        foreach (MapGrid map in Maps())
        {
            for (int row = 1; row < MapGrid.LastRow; row++)
            {
                foreach (int c in map.NodesInRow(row))
                {
                    Assert.IsNotEmpty(map.Parents(row, c), $"Node ({row},{c}) can't be reached.");
                    Assert.IsNotEmpty(map.Children(row, c), $"Node ({row},{c}) is a dead end.");
                }
            }
        }
    }

    [Test]
    public void Steps_MoveAtMostOneColumn_AndNeverCross()
    {
        foreach (MapGrid map in Maps())
        {
            // The first and last rows fan out from / into the single fixed node.
            for (int row = 1; row < MapGrid.LastRow - 1; row++)
            {
                List<(int from, int to)> edges = map.EdgesFrom(row).ToList();
                foreach (var (from, to) in edges)
                {
                    Assert.LessOrEqual(Math.Abs(from - to), 1, $"Row {row}: {from} -> {to} jumps columns.");
                    foreach (var (a, b) in edges)
                    {
                        bool crosses = (a < from && b > to) || (a > from && b < to);
                        Assert.IsFalse(crosses, $"Row {row}: {from} -> {to} crosses {a} -> {b}.");
                    }
                }
            }
        }
    }

    [Test]
    public void DrawnPaths_SumToPathCountPerRow_AndMatchActiveNodes()
    {
        var settings = new MapGenerationSettings();
        foreach (MapGrid map in Maps(settings))
        {
            for (int row = 0; row < MapGrid.Rows; row++)
            {
                int rowTotal = 0;
                for (int c = 0; c < MapGrid.Columns; c++)
                {
                    int visits = map.DrawnPathsAt(row, c);
                    Assert.AreEqual(map.IsNode(row, c), visits > 0, $"({row},{c}) node vs drawn-path mismatch.");
                    rowTotal += visits;
                }
                Assert.AreEqual(settings.pathCount, rowTotal, $"Row {row} drawn-path sum should equal pathCount.");
            }
        }
    }

    [Test]
    public void ExactlyOneShop_InRows3To6_OnNodeWithMostDrawnPaths()
    {
        var settings = new MapGenerationSettings();
        foreach (MapGrid map in Maps(settings))
        {
            var shops = AllNodes(map).Where(n => map.TypeAt(n.row, n.column) == NodeTypeEnum.Shop).ToList();
            Assert.AreEqual(1, shops.Count);
            Assert.That(shops[0].row, Is.InRange(settings.shopFirstRow, settings.shopLastRow), "GDD rows 3-6.");

            int bestDrawnPaths = Enumerable.Range(settings.shopFirstRow, settings.shopLastRow - settings.shopFirstRow + 1)
                .SelectMany(r => map.NodesInRow(r).Select(c => map.DrawnPathsAt(r, c)))
                .Max();
            Assert.AreEqual(bestDrawnPaths, map.DrawnPathsAt(shops[0].row, shops[0].column),
                "Issue #69: Shop must sit on a node visited by the most drawn S-to-F paths.");
        }
    }

    [Test]
    public void PlaceShop_UsesDrawnPathsInsteadOfMergedGraphWalks_AndBreaksTiesWithRng()
    {
        // Issue #69 regression: 3 identical drawn paths go straight down column 2 (drawnPaths = 3,
        // deduplicated DAG walks = 1), while 2 weaving paths on columns 0-1 have drawnPaths <= 2
        // at (4,0) even if combinatorial DAG walks there would be higher.
        MapGrid BuildCounterexample()
        {
            var grid = new MapGrid();
            int[][] paths =
            {
                new[] { 0, 0, 0, 0, 0, 0, 0, 0 },
                new[] { 0, 1, 0, 1, 0, 1, 0, 0 },
                new[] { 0, 2, 2, 2, 2, 2, 2, 0 },
                new[] { 0, 2, 2, 2, 2, 2, 2, 0 },
                new[] { 0, 2, 2, 2, 2, 2, 2, 0 }
            };

            foreach (int[] path in paths)
            {
                grid.AddDrawnPath(0, path[0]);
                for (int row = 0; row < MapGrid.LastRow; row++)
                {
                    grid.Connect(row, path[row], path[row + 1]);
                    grid.AddDrawnPath(row + 1, path[row + 1]);
                }
            }
            return grid;
        }

        var settings = new MapGenerationSettings { pathCount = 5, shopFirstRow = 2, shopLastRow = 5 };
        var chosenRows = new HashSet<int>();
        for (int seed = 0; seed < 50; seed++)
        {
            MapGrid grid = BuildCounterexample();
            var shop = MapGenerator.PlaceShop(grid, new Random(seed), settings);
            Assert.AreEqual(2, shop.column, "Must pick column 2 (3 drawn paths), never (4,0) (2 drawn paths, 8 DAG walks).");
            Assert.That(shop.row, Is.InRange(2, 5));
            chosenRows.Add(shop.row);
        }

        Assert.Greater(chosenRows.Count, 1, "Ties among equally visited drawn-path nodes should break randomly.");
    }

    [Test]
    public void Treasure_NeverInRow2_AndNeverRightAfterAnotherTreasure()
    {
        foreach (MapGrid map in Maps())
        {
            foreach (var (row, column) in AllNodes(map))
            {
                if (map.TypeAt(row, column) != NodeTypeEnum.Treasure) continue;

                Assert.AreNotEqual(1, row, "No Treasure in GDD row 2.");
                foreach (int parent in map.Parents(row, column))
                {
                    Assert.AreNotEqual(NodeTypeEnum.Treasure, map.TypeAt(row - 1, parent),
                        $"Treasure ({row},{column}) follows another Treasure.");
                }
            }
        }
    }

    [Test]
    public void Elite_NeverInRows2Or3_NeverConsecutive_AndAtMostMaxPerMap()
    {
        var defaultSettings = new MapGenerationSettings();
        var heavyEliteSettings = new MapGenerationSettings { eliteWeight = 200 };

        foreach (var settings in new[] { defaultSettings, heavyEliteSettings })
        {
            foreach (MapGrid map in Maps(settings))
            {
                var elites = AllNodes(map).Where(n => map.TypeAt(n.row, n.column) == NodeTypeEnum.Elite).ToList();
                Assert.LessOrEqual(elites.Count, settings.maxElitesPerMap, "Too many Elites on one map.");

                foreach (var (row, column) in elites)
                {
                    Assert.GreaterOrEqual(row, settings.eliteFirstRow, $"Elite ({row},{column}) spawned before eliteFirstRow.");
                    Assert.That(row, Is.Not.EqualTo(1).And.Not.EqualTo(2), "Elite never spawns in GDD rows 2-3.");
                    foreach (int parent in map.Parents(row, column))
                    {
                        Assert.AreNotEqual(NodeTypeEnum.Elite, map.TypeAt(row - 1, parent),
                            $"Elite ({row},{column}) directly follows another Elite.");
                    }
                }
            }
        }
    }

    [Test]
    public void RowsOfTwoOrMore_NeverAllTheSameType()
    {
        foreach (MapGrid map in Maps())
        {
            for (int row = 1; row < MapGrid.LastRow; row++)
            {
                List<NodeTypeEnum> types = map.NodesInRow(row)
                    .Select(c => map.TypeAt(row, c))
                    .Where(t => t != NodeTypeEnum.Shop)
                    .ToList();
                if (types.Count < 2) continue;

                Assert.Greater(types.Distinct().Count(), 1, $"Row {row}: every node is {types[0]}.");
            }
        }
    }

    [Test]
    public void MiddleRows_RollOnlyGddNodeTypes_AndCoverAllNewTypesAcrossSeeds()
    {
        var allowed = new[]
        {
            NodeTypeEnum.Combat,
            NodeTypeEnum.Health,
            NodeTypeEnum.Treasure,
            NodeTypeEnum.Mystery,
            NodeTypeEnum.Elite,
            NodeTypeEnum.Shop
        };

        var counts = allowed.ToDictionary(t => t, _ => 0);
        foreach (MapGrid map in Maps())
        {
            foreach (var (row, column) in AllNodes(map).Where(n => n.row > 0 && n.row < MapGrid.LastRow))
            {
                NodeTypeEnum type = map.TypeAt(row, column);
                CollectionAssert.Contains(allowed, type);
                counts[type]++;
            }
        }

        Assert.Greater(counts[NodeTypeEnum.Combat], counts[NodeTypeEnum.Health]);
        Assert.Greater(counts[NodeTypeEnum.Health], 0);
        Assert.Greater(counts[NodeTypeEnum.Treasure], 0);
        Assert.Greater(counts[NodeTypeEnum.Mystery], 0);
        Assert.Greater(counts[NodeTypeEnum.Elite], 0);
        Assert.AreEqual(Seeds, counts[NodeTypeEnum.Shop]);
    }

    [Test]
    public void SameSeed_SameMap()
    {
        MapGrid a = MapGenerator.Generate(new Random(42));
        MapGrid b = MapGenerator.Generate(new Random(42));
        foreach (var (row, column) in AllNodes(a))
        {
            Assert.IsTrue(b.IsNode(row, column));
            Assert.AreEqual(a.TypeAt(row, column), b.TypeAt(row, column));
            Assert.AreEqual(a.DrawnPathsAt(row, column), b.DrawnPathsAt(row, column));
        }
        Assert.AreEqual(AllNodes(a).Count(), AllNodes(b).Count());
        for (int row = 0; row < MapGrid.LastRow; row++)
        {
            CollectionAssert.AreEquivalent(a.EdgesFrom(row).ToList(), b.EdgesFrom(row).ToList());
        }
    }

    [Test]
    public void Weights_ControlTheNodeTypes()
    {
        var onlyCombat = new MapGenerationSettings
        {
            healthWeight = 0,
            treasureWeight = 0,
            mysteryWeight = 0,
            eliteWeight = 0,
            pathCount = 1
        };
        MapGrid map = MapGenerator.Generate(new Random(7), onlyCombat);
        foreach (var (row, column) in AllNodes(map).Where(n => n.row > 0 && n.row < MapGrid.LastRow))
        {
            Assert.That(map.TypeAt(row, column), Is.EqualTo(NodeTypeEnum.Combat).Or.EqualTo(NodeTypeEnum.Shop));
        }
    }

    [Test]
    public void MapConfigAsset_MatchesGddDefaults()
    {
        var config = AssetDatabase.LoadAssetAtPath<MapConfig>("Assets/_Game/Data/Map/MapConfig.asset");
        Assert.IsNotNull(config, "Expected MapConfig.asset at Assets/_Game/Data/Map/MapConfig.asset.");

        Assert.AreEqual(4, config.generation.pathCount);
        Assert.AreEqual(2, config.generation.shopFirstRow);
        Assert.AreEqual(5, config.generation.shopLastRow);
        Assert.AreEqual(3, config.generation.eliteFirstRow);
        Assert.AreEqual(2, config.generation.maxElitesPerMap);
        Assert.AreEqual(40, config.generation.combatWeight);
        Assert.AreEqual(20, config.generation.healthWeight);
        Assert.AreEqual(15, config.generation.treasureWeight);
        Assert.AreEqual(15, config.generation.mysteryWeight);
        Assert.AreEqual(10, config.generation.eliteWeight);

        Assert.AreEqual(0.25f, config.rooms.restRoomChance, 1e-5f);
        Assert.AreEqual(0.15f, config.rooms.shopRoomChance, 1e-5f);
        Assert.AreEqual(60f, config.rooms.healthNodeHeal, 1e-5f);
        Assert.AreEqual(50f, config.rooms.treasureHeal, 1e-5f);
        Assert.AreEqual(30, config.rooms.mysteryFightWeight);
        Assert.AreEqual(25, config.rooms.mysteryTreasureWeight);
        Assert.AreEqual(25, config.rooms.mysteryDealWeight);
        Assert.AreEqual(10, config.rooms.mysteryEliteWeight);
        Assert.AreEqual(10, config.rooms.mysteryNarrativeWeight);
        Assert.AreEqual(0.40f, config.rooms.mysteryEliteMinHealthRatio, 1e-5f);
    }

    [Test]
    public void RoomDoorTransition_ExitTrigger_CoversStandardAndLayout004FloorHeights()
    {
        // Issue #73: SetScene places the door at (0, 0.25, 13.1) facing -Z (180 Y); Layout_004 left the player at y = 2.59.
        var doorObj = new GameObject("TestDoor");
        try
        {
            doorObj.transform.position = new Vector3(0f, 0.25f, 13.1f);
            doorObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            RoomDoorTransition door = doorObj.AddComponent<RoomDoorTransition>();

            BoxCollider zone = new GameObject("Trigger").AddComponent<BoxCollider>();
            zone.transform.SetParent(doorObj.transform, false);
            door.ConfigureExitTrigger(zone, Vector3.forward * 1f + Vector3.up * 0.02f);
            Physics.SyncTransforms();

            Assert.IsTrue(zone.isTrigger);
            Assert.IsTrue(zone.bounds.Contains(new Vector3(0f, 0.5f, 12.1f)), "Standard floor height must sit inside exit trigger.");
            Assert.IsTrue(zone.bounds.Contains(new Vector3(0f, 2.59f, 12.1f)), "Issue #73: Layout_004 player height (y=2.59) must sit inside exit trigger.");
            Assert.IsFalse(zone.bounds.Contains(new Vector3(3f, 2.59f, 12.1f)), "Off-mark XZ positions must not trigger the exit.");
        }
        finally
        {
            Object.DestroyImmediate(doorObj);
        }
    }

    private static IEnumerable<(int row, int column)> AllNodes(MapGrid map)
    {
        for (int row = 0; row < MapGrid.Rows; row++)
        {
            foreach (int c in map.NodesInRow(row))
            {
                yield return (row, c);
            }
        }
    }
}
