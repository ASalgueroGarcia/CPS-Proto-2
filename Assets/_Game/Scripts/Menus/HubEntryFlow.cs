using System;

/// <summary>
/// Wraps the game's IGameFlow so that entering the hub first plays a camera flight (the title
/// screen camera flies into the wagon's lobby), then hands over to the real flow. Main menu and
/// quit pass straight through. The presenters don't know the flight exists.
/// </summary>
public class HubEntryFlow : IGameFlow
{
    private readonly IGameFlow _inner;
    private readonly ICameraFlight _flight;
    private bool _entering;

    public HubEntryFlow(IGameFlow inner, ICameraFlight flight)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _flight = flight;
    }

    public void EnterHub()
    {
        if (_flight == null)
        {
            _inner.EnterHub();
            return;
        }

        if (_entering) return;
        _entering = true;
        _flight.Play(_inner.EnterHub);
    }

    public void EnterMainMenu() => _inner.EnterMainMenu();

    public void Quit() => _inner.Quit();
}
