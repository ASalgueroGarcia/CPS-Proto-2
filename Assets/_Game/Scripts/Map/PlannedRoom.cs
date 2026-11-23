public readonly struct PlannedRoom
{
    public readonly RoomType Room;
    public readonly RoomRewards? Rewards;
    public readonly EncounterContext? Encounter;

    public bool HasSetup => Encounter.HasValue || Rewards.HasValue;

    public PlannedRoom(RoomType room, RoomRewards? rewards = null, EncounterContext? encounter = null)
    {
        Room = room;
        Rewards = rewards;
        Encounter = encounter;
    }

    public void Apply(WaveManager waves)
    {
        if (Encounter.HasValue) waves.SetEncounterContext(Encounter.Value);
        if (Rewards.HasValue) Rewards.Value.Apply(waves);
    }
}
