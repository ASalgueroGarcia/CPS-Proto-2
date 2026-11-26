public readonly struct PlannedRoom
{
    public readonly RoomType Room;
    public readonly RoomRewards? Rewards;
    public readonly EncounterContext? Encounter;
    public readonly MysteryOutcome? Mystery;
    public readonly SpecialRoomKind SpecialRoom;

    public bool HasSetup => Encounter.HasValue || Rewards.HasValue;
    public bool IsElite => Encounter.HasValue && Encounter.Value.IsElite;
    public bool IsCalmSpecialRoom => Room == RoomType.Shop || SpecialRoom != SpecialRoomKind.None;

    public PlannedRoom(
        RoomType room,
        RoomRewards? rewards = null,
        EncounterContext? encounter = null,
        MysteryOutcome? mystery = null,
        SpecialRoomKind specialRoom = SpecialRoomKind.None)
    {
        Room = room;
        Rewards = rewards;
        Encounter = encounter;
        Mystery = mystery;
        SpecialRoom = specialRoom;
    }

    public void Apply(WaveManager waves)
    {
        if (Encounter.HasValue) waves.SetEncounterContext(Encounter.Value);
        if (Rewards.HasValue) Rewards.Value.Apply(waves);
    }
}
