/// <summary>
/// Anything a weapon can break by hitting it (crates, props). Lives in Core so the
/// player can hit breakables without depending on the Obstacles assembly.
/// </summary>
public interface IBreakable
{
    void TakeDamage(int dmg);
}
