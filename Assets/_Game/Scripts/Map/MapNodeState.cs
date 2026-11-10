/// <summary>How a map node relates to the player's route so far.</summary>
public enum MapNodeState
{
    /// <summary>The node the player is on.</summary>
    Current,
    /// <summary>Can be entered next.</summary>
    Available,
    /// <summary>Still reachable further up the map.</summary>
    Later,
    /// <summary>Already passed, or no longer reachable.</summary>
    Closed
}