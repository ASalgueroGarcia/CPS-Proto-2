/// <summary>One room of a map node: its type and, when the node changes them, its rewards.</summary>
public readonly struct PlannedRoom
{
    public readonly RoomType Room;
    public readonly RoomRewards? Rewards;

    public PlannedRoom(RoomType room, RoomRewards? rewards = null)
    {
        Room = room;
        Rewards = rewards;
    }
}