using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The scene map rules (GDD > Level Design > Scene Map), checked over many seeds.</summary>
public class MapGeneratorTests
{
    private const int Seeds = 300;

    private static IEnumerable<MapGrid> Maps()
    {
        for (int seed = 0; seed < Seeds; seed++)
            yield return MapGenerator.Generate(new System.Random(seed));
    }

    private static IEnumerable<(int row, int column)> Nodes(MapGrid map)
    {
        for (int row = 0; row < MapGrid.Rows; row++)
            foreach (int column in map.NodesInRow(row))
                yield return (row, column);
    }

    [Test]
    public void First_and_last_rows_hold_one_node()
    {
        foreach (MapGrid map in Maps())
        {
            Assert.AreEqual(1, map.NodesInRow(0).Count);
            Assert.AreEqual(1, map.NodesInRow(MapGrid.LastRow).Count);
            Assert.AreEqual(NodeTypeEnum.Combat, map.TypeAt(0, 0));
            Assert.AreEqual(NodeTypeEnum.FinalFight, map.TypeAt(MapGrid.LastRow, 0));
        }
    }

    [Test]
    public void Second_row_has_at_least_two_nodes()
    {
        foreach (MapGrid map in Maps())
            Assert.That(map.NodesInRow(1).Count, Is.InRange(2, MapGrid.Columns));
    }

    [Test]
    public void Every_node_is_on_a_path_from_start_to_final_fight()
    {
        foreach (MapGrid map in Maps())
        {
            foreach (var (row, column) in Nodes(map))
            {
                if (row > 0) Assert.IsNotEmpty(map.Parents(row, column), $"node {row},{column} has no way in");
                if (row < MapGrid.LastRow) Assert.IsNotEmpty(map.Children(row, column), $"node {row},{column} is a dead end");
            }
        }
    }

    [Test]
    public void Connections_never_cross()
    {
        foreach (MapGrid map in Maps())
        {
            for (int row = 0; row < MapGrid.LastRow; row++)
            {
                var edges = map.EdgesFrom(row).ToList();
                foreach (var a in edges)
                    foreach (var b in edges)
                        Assert.IsFalse(a.from < b.from && a.to > b.to, $"row {row}: {a} crosses {b}");
            }
        }
    }

    [Test]
    public void Exactly_one_shop_on_the_middle_rows()
    {
        var settings = new MapGenerationSettings();
        foreach (MapGrid map in Maps())
        {
            var shops = Nodes(map).Where(n => map.TypeAt(n.row, n.column) == NodeTypeEnum.Shop).ToList();
            Assert.AreEqual(1, shops.Count);
            Assert.That(shops[0].row, Is.InRange(settings.shopFirstRow, settings.shopLastRow));
        }
    }

    [Test]
    public void Treasure_is_never_on_the_second_row_or_right_after_treasure()
    {
        foreach (MapGrid map in Maps())
        {
            foreach (var (row, column) in Nodes(map).Where(n => map.TypeAt(n.row, n.column) == NodeTypeEnum.Treasure))
            {
                Assert.AreNotEqual(1, row);
                Assert.IsFalse(map.Parents(row, column).Any(p => map.TypeAt(row - 1, p) == NodeTypeEnum.Treasure),
                    $"treasure {row},{column} follows a treasure");
            }
        }
    }

    [Test]
    public void A_row_is_never_all_one_type()
    {
        foreach (MapGrid map in Maps())
        {
            for (int row = 1; row < MapGrid.LastRow; row++)
            {
                var types = map.NodesInRow(row).Select(c => map.TypeAt(row, c)).Where(t => t != NodeTypeEnum.Shop).ToList();
                if (types.Count >= 2) Assert.Greater(types.Distinct().Count(), 1, $"row {row} is all {types[0]}");
            }
        }
    }

    [Test]
    public void Same_seed_gives_the_same_map()
    {
        MapGrid a = MapGenerator.Generate(new System.Random(42));
        MapGrid b = MapGenerator.Generate(new System.Random(42));
        foreach (var (row, column) in Nodes(a))
        {
            Assert.IsTrue(b.IsNode(row, column));
            Assert.AreEqual(a.TypeAt(row, column), b.TypeAt(row, column));
            CollectionAssert.AreEquivalent(a.Children(row, column), b.Children(row, column));
        }
    }

    [Test]
    public void Weights_control_the_node_types()
    {
        var onlyCombat = new MapGenerationSettings { moreHealthWeight = 0, moreButtonsWeight = 0, treasureWeight = 0, pathCount = 1 };
        MapGrid map = MapGenerator.Generate(new System.Random(7), onlyCombat);
        foreach (var (row, column) in Nodes(map).Where(n => n.row > 0 && n.row < MapGrid.LastRow))
            Assert.That(map.TypeAt(row, column), Is.EqualTo(NodeTypeEnum.Combat).Or.EqualTo(NodeTypeEnum.Shop));
    }
}
