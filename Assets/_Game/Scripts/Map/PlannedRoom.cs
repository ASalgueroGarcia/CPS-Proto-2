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