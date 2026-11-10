using System;
using System.Collections.Generic;

public static class MapGenerator
{
    /// <summary>Rolls a map with the default numbers.</summary>
    public static MapGrid Generate(Random rng) => Generate(rng, new MapGenerationSettings());

    /// <summary>Rolls a map. The same seed and settings always give the same map.</summary>
    public static MapGrid Generate(Random rng, MapGenerationSettings settings)
    {
        MapGrid map = new MapGrid();
        DrawPaths(map, rng, settings.pathCount);
        var shop = PlaceShop(map, rng, settings);
        RollTypes(map, rng, shop, settings);
        map.SetType(0, 0, NodeTypeEnum.Combat);
        map.SetType(MapGrid.LastRow, 0, NodeTypeEnum.FinalFight);
        return map;
    }

    private static void DrawPaths(MapGrid map, Random rng, int pathCount)
    {
        int firstColumn = -1;
        for (int p = 0; p < pathCount; p++)
        {
            int column;
            do
            {
                column = rng.Next(MapGrid.Columns);
            }
            while (p == 1 && column == firstColumn);

            if (p == 0) firstColumn = column;

            map.Connect(0, 0, column);
            for (int row = 1; row < MapGrid.LastRow - 1; row++)
            {
                List<int> options = new List<int>();
                for (int next = column - 1; next <= column + 1; next++)
                {
                    if (next >= 0 && next < MapGrid.Columns && !Crosses(map, row, column, next))
                    {
                        options.Add(next);
                    }
                }

                int chosen = options[rng.Next(options.Count)];
                map.Connect(row, column, chosen);
                column = chosen;
            }
            map.Connect(MapGrid.LastRow - 1, column, 0);
        }
    }

    private static bool Crosses(MapGrid map, int row, int from, int to)
    {
        foreach (var (a, b) in map.EdgesFrom(row))
        {
            if ((a < from && b > to) || (a > from && b < to))
            {
                return true;
            }
        }
        return false;
    }

    private static (int row, int column) PlaceShop(MapGrid map, Random rng, MapGenerationSettings settings)
    {
        long[,] fromStart = new long[MapGrid.Rows, MapGrid.Columns];
        long[,] toEnd = new long[MapGrid.Rows, MapGrid.Columns];
        fromStart[0, 0] = 1;
        toEnd[MapGrid.LastRow, 0] = 1;

        for (int row = 1; row < MapGrid.Rows; row++)
        {
            foreach (int c in map.NodesInRow(row))
            {
                foreach (int parent in map.Parents(row, c))
                {
                    fromStart[row, c] += fromStart[row - 1, parent];
                }
            }
        }

        for (int row = MapGrid.LastRow - 1; row >= 0; row--)
        {
            foreach (int c in map.NodesInRow(row))
            {
                foreach (int child in map.Children(row, c))
                {
                    toEnd[row, c] += toEnd[row + 1, child];
                }
            }
        }

        List<(int, int)> best = new List<(int, int)>();
        long bestCount = -1;
        for (int row = settings.shopFirstRow; row <= settings.shopLastRow; row++)
        {
            foreach (int c in map.NodesInRow(row))
            {
                long count = fromStart[row, c] * toEnd[row, c];
                if (count > bestCount)
                {
                    bestCount = count;
                    best.Clear();
                }
                if (count == bestCount)
                {
                    best.Add((row, c));
                }
            }
        }

        var shop = best[rng.Next(best.Count)];
        map.SetType(shop.Item1, shop.Item2, NodeTypeEnum.Shop);
        return shop;
    }

    private static void RollTypes(MapGrid map, Random rng, (int row, int column) shop, MapGenerationSettings settings)
    {
        for (int row = 1; row < MapGrid.LastRow; row++)
        {
            List<int> columns = map.NodesInRow(row);
            columns.Remove(shop.row == row ? shop.column : -1);

            foreach (int c in columns)
            {
                map.SetType(row, c, RollType(map, rng, row, c, settings));
            }

            if (columns.Count < 2) continue;
            NodeTypeEnum first = map.TypeAt(row, columns[0]);
            if (!columns.TrueForAll(c => map.TypeAt(row, c) == first)) continue;

            int last = columns[columns.Count - 1];
            while (map.TypeAt(row, last) == first)
            {
                map.SetType(row, last, RollType(map, rng, row, last, settings));
            }
        }
    }

    private static NodeTypeEnum RollType(MapGrid map, Random rng, int row, int column, MapGenerationSettings settings)
    {
        int combat = settings.combatWeight;
        int moreHealth = combat + settings.moreHealthWeight;
        int moreButtons = moreHealth + settings.moreButtonsWeight;
        int total = moreButtons + settings.treasureWeight;

        while (true)
        {
            int roll = rng.Next(total);
            NodeTypeEnum type = roll < combat ? NodeTypeEnum.Combat
                : roll < moreHealth ? NodeTypeEnum.MoreHealth
                : roll < moreButtons ? NodeTypeEnum.MoreButtons
                : NodeTypeEnum.Treasure;

            if (type == NodeTypeEnum.Treasure && (row == 1 || FollowsTreasure(map, row, column))) continue;
            return type;
        }
    }

    private static bool FollowsTreasure(MapGrid map, int row, int column)
    {
        return map.Parents(row, column).Exists(p => map.TypeAt(row - 1, p) == NodeTypeEnum.Treasure);
    }
}
