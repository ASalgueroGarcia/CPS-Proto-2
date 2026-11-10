using NUnit.Framework;

/// <summary>Node states as the player moves up the map: one node per row, no going back.</summary>
public class MapProgressTests
{
    // Row 0: one node. Row 1: columns 0 and 2. Row 2: columns 0, 1 and 3. Then single file to the end.
    //   (0,0) -> (1,0) -> (2,0), (2,1)
    //   (0,0) -> (1,2) -> (2,3)
    private static MapGrid Map()
    {
        var map = new MapGrid();
        map.Connect(0, 0, 0);
        map.Connect(0, 0, 2);
        map.Connect(1, 0, 0);
        map.Connect(1, 0, 1);
        map.Connect(1, 2, 3);
        map.Connect(2, 0, 0);
        map.Connect(2, 1, 0);
        map.Connect(2, 3, 0);
        for (int row = 3; row < MapGrid.LastRow; row++) map.Connect(row, 0, 0);
        return map;
    }

    [Test]
    public void Only_the_first_node_is_available_at_the_start()
    {
        var progress = new MapProgress(Map());

        Assert.AreEqual(MapNodeState.Available, progress.StateOf(0, 0));
        Assert.AreEqual(MapNodeState.Later, progress.StateOf(1, 0));
        Assert.AreEqual(MapNodeState.Later, progress.StateOf(2, 3));
        Assert.AreEqual(MapNodeState.Later, progress.StateOf(MapGrid.LastRow, 0));
    }

    [Test]
    public void Entering_a_node_makes_it_current_and_opens_its_children()
    {
        var progress = new MapProgress(Map());
        progress.Enter(0);

        Assert.AreEqual(MapNodeState.Current, progress.StateOf(0, 0));
        Assert.AreEqual(MapNodeState.Available, progress.StateOf(1, 0));
        Assert.AreEqual(MapNodeState.Available, progress.StateOf(1, 2));
        Assert.AreEqual(MapNodeState.Later, progress.StateOf(2, 1));
    }

    [Test]
    public void Choosing_a_branch_closes_the_other_one()
    {
        var progress = new MapProgress(Map());
        progress.Enter(0);
        progress.Enter(0);

        Assert.AreEqual(MapNodeState.Closed, progress.StateOf(0, 0));
        Assert.AreEqual(MapNodeState.Current, progress.StateOf(1, 0));
        Assert.AreEqual(MapNodeState.Closed, progress.StateOf(1, 2));
        Assert.AreEqual(MapNodeState.Closed, progress.StateOf(2, 3));
        Assert.AreEqual(MapNodeState.Available, progress.StateOf(2, 0));
        Assert.AreEqual(MapNodeState.Available, progress.StateOf(2, 1));
        Assert.AreEqual(MapNodeState.Later, progress.StateOf(3, 0));
    }

    [Test]
    public void Connections_are_traversed_ahead_or_neither()
    {
        var progress = new MapProgress(Map());
        progress.Enter(0);
        progress.Enter(0);

        Assert.IsTrue(progress.IsTraversed(0, 0, 0));
        Assert.IsFalse(progress.IsTraversed(0, 0, 2));
        Assert.IsTrue(progress.IsAhead(1, 0, 1));
        Assert.IsFalse(progress.IsAhead(1, 2, 3));
        Assert.IsFalse(progress.IsAhead(0, 0, 2));
    }

    [Test]
    public void Stepping_back_reopens_the_node()
    {
        var progress = new MapProgress(Map());
        progress.Enter(0);
        progress.Enter(2);
        progress.StepBack();

        Assert.AreEqual(MapNodeState.Current, progress.StateOf(0, 0));
        Assert.AreEqual(MapNodeState.Available, progress.StateOf(1, 2));
        Assert.AreEqual(MapNodeState.Available, progress.StateOf(1, 0));
    }

    [Test]
    public void Stepping_back_at_the_start_does_nothing()
    {
        var progress = new MapProgress(Map());
        progress.StepBack();

        Assert.AreEqual(MapNodeState.Available, progress.StateOf(0, 0));
    }
}
