using System;

/// <summary>
/// A scripted camera move that runs once and then reports back, e.g. the title screen camera
/// flying into the wagon before the game enters the hub. HubEntryFlow plays it.
/// </summary>
public interface ICameraFlight
{
    /// <summary>Plays the move; <paramref name="onFinished"/> runs once the camera has arrived.</summary>
    void Play(Action onFinished);
}
