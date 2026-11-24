/// <summary>What reading a save or settings file found.</summary>
public enum SaveParseResult
{
    Ok,
    Corrupt,
    /// <summary>Written by a newer build. Readable, but must never be overwritten by this one.</summary>
    NewerVersion,
}
