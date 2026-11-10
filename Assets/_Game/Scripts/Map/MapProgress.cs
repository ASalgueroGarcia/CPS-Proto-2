using System.Collections.Generic;

/// <summary>
/// The player's route through one map: which node they are on, which they can enter next,
/// and which are still reachable. Plain C#, no scene access.
/// </summary>
public class MapProgress
{
    private readonly MapGrid map;
    private readonly List<int> path = new List<int>();
    private readonly HashSet<(int row, int column)> available = new HashSet<(int, int)>();
    private readonly HashSet<(int row, int column)> reachable = new HashSet<(int, int)>();

    public MapProgress(MapGrid map)
    {
        this.map = map;
        Recompute();
    }

    /// <summary>Moves onto a node of the next row. Only the column is needed: one node is entered per row.</summary>
    public void Enter(int column)
    {
        path.Add(column);
        Recompute();
    }

    /// <summary>Takes back the last Enter, for a node the player left without finishing.</summary>
    public void StepBack()
    {
        if (path.Count == 0) return;
        path.RemoveAt(path.Count - 1);
        Recompute();
    }

    public bool OnPath(int row, int column) => row < path.Count && path[row] == column;

    public MapNodeState StateOf(int row, int column)
    {
        if (OnPath(row, column)) return row == path.Count - 1 ? MapNodeState.Current : MapNodeState.Closed;
        if (available.Contains((row, column))) return MapNodeState.Available;
        return reachable.Contains((row, column)) ? MapNodeState.Later : MapNodeState.Closed;
    }

    /// <summary>True for a connection the player has walked.</summary>
    public bool IsTraversed(int row, int from, int to) => OnPath(row, from) && OnPath(row + 1, to);

    /// <summary>True for a connection the player can still take.</summary>
    public bool IsAhead(int row, int from, int to) =>
        (OnPath(row, from) || reachable.Contains((row, from))) && reachable.Contains((row + 1, to));

    private void Recompute()
    {
        available.Clear();
        if (path.Count == 0)
        {
            available.Add((0, 0));
        }
        else
        {
            int row = path.Count - 1;
            foreach (int child in map.Children(row, path[row]))
            {
                available.Add((row + 1, child));
            }
        }

        reachable.Clear();
        reachable.UnionWith(available);
        for (int row = path.Count; row < MapGrid.LastRow; row++)
        {
            foreach (var (from, to) in map.EdgesFrom(row))
            {
                if (reachable.Contains((row, from)))
                {
                    reachable.Add((row + 1, to));
                }
            }
        }
    }
}