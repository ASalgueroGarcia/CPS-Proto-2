/// <summary>
/// Anything that shows its own flash when Health takes a hit (enemies, via
/// EnemyVisualFeedback). Lives in Core so Health doesn't depend on the Enemies assembly.
/// </summary>
public interface IHitFlash
{
    void ShowHitFlash();
}
