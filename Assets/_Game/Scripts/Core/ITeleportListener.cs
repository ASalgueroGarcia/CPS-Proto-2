/// <summary>
/// Anything on the player that must reset when the player is moved instantly (the scarf's
/// cloth, via ClothReset). Lives in Core so the Map's door can tell it without depending on
/// the Costume assembly.
/// </summary>
public interface ITeleportListener
{
    void OnTeleported();
}
