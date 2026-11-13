public enum MapNodeState
{
    Current,
    Available,
    /// <summary>Still reachable further up the map.</summary>
    Later,
    /// <summary>Already passed, or no longer reachable.</summary>
    Closed
}