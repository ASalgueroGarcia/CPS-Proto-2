using System.Collections.Generic;

public class MapGrid
{
    public const int Rows = 8;
    public const int Columns = 4;
    public const int LastRow = Rows - 1;

    private readonly bool[,] nodes = new bool[Rows, Columns];
    private readonly NodeTypeEnum[,] types = new NodeTypeEnum[Rows, Columns];
    private readonly HashSet<(int from, int to)>[] edges = new HashSet<(int, int)>[Rows - 1];

    public MapGrid()
    {
        for (int i = 0; i < edges.Length; i++)
        {
            edges[i] = new HashSet<(int, int)>();
        }
    }

    public bool IsNode(int row, int column)
    {
        return nodes[row, column];
    }

    public NodeTypeEnum TypeAt(int row, int column)
    {
        return types[row, column];
    }

    public void SetType(int row, int column, NodeTypeEnum type)
    {
        types[row, column] = type;
    }

    public IEnumerable<(int from, int to)> EdgesFrom(int row)
    {
        return edges[row];
    }

    public void Connect(int row, int from, int to)
    {
        nodes[row, from] = true;
        nodes[row + 1, to] = true;
        edges[row].Add((from, to));
    }

    public List<int> NodesInRow(int row)
    {
        List<int> result = new List<int>();
        for (int c = 0; c < Columns; c++)
        {
            if (nodes[row, c]) result.Add(c);
        }
        return result;
    }

    public List<int> Children(int row, int column)
    {
        List<int> result = new List<int>();
        if (row >= LastRow) return result;

        foreach (var (from, to) in edges[row])
        {
            if (from == column) result.Add(to);
        }
        return result;
    }

    public List<int> Parents(int row, int column)
    {
        List<int> result = new List<int>();
        if (row <= 0) return result;

        foreach (var (from, to) in edges[row - 1])
        {
            if (to == column) result.Add(from);
        }
        return result;
    }
}
