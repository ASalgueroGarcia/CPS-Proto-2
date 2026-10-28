using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// MapGenerator against the GDD Scene Map rules (Level Design > In-Run > "In-Run in Depth" > Scene Map),
/// over many seeds. Rows here are 0-based: GDD row 1 = row 0, row 8 = MapGrid.LastRow.
/// </summary>
public class MapGeneratorTests
{
    private const int Seeds = 500;

    private static IEnumerable<MapGrid> Maps()
    {
        for (int seed = 0; seed < Seeds; seed++)
        {
            yield return MapGenerator.Generate(new Random(seed));
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
    public void ExactlyOneShop_InRows3To6()
    {
        foreach (MapGrid map in Maps())
        {
            var shops = AllNodes(map).Where(n => map.TypeAt(n.row, n.column) == NodeTypeEnum.Shop).ToList();
            Assert.AreEqual(1, shops.Count);
            Assert.That(shops[0].row, Is.InRange(2, 5), "GDD rows 3-6.");
        }
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
    public void MiddleRows_OnlyRollFightHealthButtonsOrTreasure()
    {
        var allowed = new[] { NodeTypeEnum.Combat, NodeTypeEnum.MoreHealth, NodeTypeEnum.MoreButtons, NodeTypeEnum.Treasure, NodeTypeEnum.Shop };
        foreach (MapGrid map in Maps())
        {
            foreach (var (row, column) in AllNodes(map).Where(n => n.row > 0 && n.row < MapGrid.LastRow))
            {
                CollectionAssert.Contains(allowed, map.TypeAt(row, column));
            }
        }
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
        }
        Assert.AreEqual(AllNodes(a).Count(), AllNodes(b).Count());
        for (int row = 0; row < MapGrid.LastRow; row++)
        {
            CollectionAssert.AreEquivalent(a.EdgesFrom(row).ToList(), b.EdgesFrom(row).ToList());
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
