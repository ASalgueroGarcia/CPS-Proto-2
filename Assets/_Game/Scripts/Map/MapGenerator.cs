using System;
using System.Collections.Generic;

public static class MapGenerator
{
    private static readonly NodeTypeEnum[] RollableTypes =
    {
        NodeTypeEnum.Combat,
        NodeTypeEnum.Health,
        NodeTypeEnum.Treasure,
        NodeTypeEnum.Mystery,
        NodeTypeEnum.Elite
    };

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

    /// <summary>
    /// Places the single Shop node on the node in shopFirstRow..shopLastRow visited by the most
    /// drawn S-to-F paths, breaking ties randomly with <paramref name="rng"/>.
    /// </summary>
    public static (int row, int column) PlaceShop(MapGrid map, Random rng, MapGenerationSettings settings)
    {
        List<(int row, int column)> best = new List<(int, int)>();
        int bestCount = -1;
        int firstRow = Math.Max(1, settings.shopFirstRow);
        int lastRow = Math.Min(MapGrid.LastRow - 1, settings.shopLastRow);

        for (int row = firstRow; row <= lastRow; row++)
        {
            foreach (int c in map.NodesInRow(row))
            {
                int count = map.DrawnPathsAt(row, c);
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
        map.SetType(shop.row, shop.column, NodeTypeEnum.Shop);
        return shop;
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
            map.AddDrawnPath(0, 0);
            map.AddDrawnPath(1, column);

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
                map.AddDrawnPath(row + 1, column);
            }

            map.Connect(MapGrid.LastRow - 1, column, 0);
            map.AddDrawnPath(MapGrid.LastRow, 0);
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

    private static void RollTypes(MapGrid map, Random rng, (int row, int column) shop, MapGenerationSettings settings)
    {
        int eliteCount = 0;

        for (int row = 1; row < MapGrid.LastRow; row++)
        {
            List<int> columns = map.NodesInRow(row);
            columns.Remove(shop.row == row ? shop.column : -1);

            foreach (int c in columns)
            {
                NodeTypeEnum rolled = RollType(map, rng, row, c, eliteCount, settings);
                map.SetType(row, c, rolled);
                if (rolled == NodeTypeEnum.Elite) eliteCount++;
            }

            if (columns.Count < 2) continue;
            NodeTypeEnum first = map.TypeAt(row, columns[0]);
            if (!columns.TrueForAll(c => map.TypeAt(row, c) == first)) continue;

            int target = columns[columns.Count - 1];
            int candidateElites = first == NodeTypeEnum.Elite ? eliteCount - 1 : eliteCount;
            for (int i = columns.Count - 1; i >= 0; i--)
            {
                if (TotalAllowedWeight(map, row, columns[i], candidateElites, settings, first) > 0)
                {
                    target = columns[i];
                    break;
                }
            }

            if (first == NodeTypeEnum.Elite) eliteCount--;
            NodeTypeEnum rerolled = RollType(map, rng, row, target, eliteCount, settings, first);
            map.SetType(row, target, rerolled);
            if (rerolled == NodeTypeEnum.Elite) eliteCount++;
        }
    }

    private static NodeTypeEnum RollType(
        MapGrid map,
        Random rng,
        int row,
        int column,
        int eliteCount,
        MapGenerationSettings settings,
        NodeTypeEnum? exclude = null)
    {
        int total = TotalAllowedWeight(map, row, column, eliteCount, settings, exclude);
        if (total <= 0 && exclude.HasValue)
        {
            exclude = null;
            total = TotalAllowedWeight(map, row, column, eliteCount, settings, null);
        }
        if (total <= 0) return NodeTypeEnum.Combat;

        int roll = rng.Next(total);
        foreach (NodeTypeEnum type in RollableTypes)
        {
            int weight = AllowedWeight(map, row, column, type, eliteCount, settings, exclude);
            if (weight <= 0) continue;
            if (roll < weight) return type;
            roll -= weight;
        }

        return NodeTypeEnum.Combat;
    }

    private static int TotalAllowedWeight(
        MapGrid map,
        int row,
        int column,
        int eliteCount,
        MapGenerationSettings settings,
        NodeTypeEnum? exclude)
    {
        int total = 0;
        foreach (NodeTypeEnum type in RollableTypes)
        {
            total += AllowedWeight(map, row, column, type, eliteCount, settings, exclude);
        }
        return total;
    }

    private static int AllowedWeight(
        MapGrid map,
        int row,
        int column,
        NodeTypeEnum type,
        int eliteCount,
        MapGenerationSettings settings,
        NodeTypeEnum? exclude)
    {
        if (exclude.HasValue && type == exclude.Value) return 0;
        if (!IsAllowed(map, row, column, type, eliteCount, settings)) return 0;

        int weight = type switch
        {
            NodeTypeEnum.Combat => settings.combatWeight,
            NodeTypeEnum.Health => settings.healthWeight,
            NodeTypeEnum.Treasure => settings.treasureWeight,
            NodeTypeEnum.Mystery => settings.mysteryWeight,
            NodeTypeEnum.Elite => settings.eliteWeight,
            _ => 0
        };
        return Math.Max(0, weight);
    }

    private static bool IsAllowed(MapGrid map, int row, int column, NodeTypeEnum type, int eliteCount, MapGenerationSettings settings)
    {
        if (type == NodeTypeEnum.Treasure)
        {
            return row > 1 && !Follows(map, row, column, NodeTypeEnum.Treasure);
        }

        if (type == NodeTypeEnum.Elite)
        {
            return row >= settings.eliteFirstRow
                && eliteCount < settings.maxElitesPerMap
                && !Follows(map, row, column, NodeTypeEnum.Elite);
        }

        return true;
    }

    private static bool Follows(MapGrid map, int row, int column, NodeTypeEnum type)
    {
        return map.Parents(row, column).Exists(p => map.TypeAt(row - 1, p) == type);
    }
}
