/// <summary>
/// Something that blows up when set off (explosive props). Lives in Core so enemies can
/// set one off without depending on the Obstacles assembly.
/// </summary>
public interface IExplosive
{
    /// <summary>Explodes now, skipping any fuse. Safe to call more than once.</summary>
    void Detonate();
}