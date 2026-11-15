/// <summary>
/// Scene-to-scene navigation the menus and the run ask for. Core can't reach UIManager or the
/// scene code, so the UI layer implements this and registers it with Services.RegisterFlow.
/// </summary>
public interface IGameFlow
{
    /// <summary>Where the player wakes up: the lobby once it exists; until then, a new run.</summary>
    void EnterHub();

    void EnterMainMenu();

    void Quit();
}
