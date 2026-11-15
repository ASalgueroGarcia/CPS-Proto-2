/// <summary>
/// Base class for player states, providing common functionality.
/// </summary>
public abstract class PlayerStateBase
{
    protected Player player;
    protected PlayerModules Modules => player.Modules;

    public void SetPlayer(Player p) => player = p;

    public virtual void Enter() { }
    public abstract void Tick();
    public virtual void Exit() { }
    public virtual void OnAnimationEvent(string evt) { }
}