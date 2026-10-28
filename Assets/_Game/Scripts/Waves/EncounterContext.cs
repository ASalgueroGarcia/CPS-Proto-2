// Where on the run map a combat room sits. The map side (SceneController) knows this and hands
// it to WaveManager.SetEncounterContext() right after instantiating the room, before Start().
// GDD > Level Design > In-Run > "In-Run in Depth" (Encounters, Rewards, Fight Nodes & Rooms).

/// <summary>The kind of map node a combat room belongs to. Drives reward rates.</summary>
public enum EncounterNodeKind
{
    Fight,
    MoreHealth,
    MoreButtons,
    /// <summary>The scene's boss room. GDD Rewards: pays Boss Tickets (on the win), never Buttons or HP.</summary>
    FinalFight
}

[System.Serializable]
public struct EncounterContext
{
    /// <summary>Scene inside the act, 1-based (1, 2, 3). Gates enemies and picks the weight column.</summary>
    public int scene;

    public EncounterNodeKind nodeKind;

    /// <summary>True for the map's row-1 node: simplified pool with per-enemy First Spot caps.</summary>
    public bool isFirstSpot;

    /// <summary>Room inside its node, 0-based. Rooms get harder every two rooms.</summary>
    public int roomInNode;

    public EncounterContext(int scene, EncounterNodeKind nodeKind, bool isFirstSpot, int roomInNode)
    {
        this.scene = scene;
        this.nodeKind = nodeKind;
        this.isFirstSpot = isFirstSpot;
        this.roomInNode = roomInNode;
    }
}
