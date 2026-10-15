/// <summary>How a run ended. Quitting isn't an outcome: an abandoned run saves nothing.</summary>
public enum RunOutcome
{
    Death,
    /// <summary>The scene's final fight was won (mini-boss in scenes 1-2, act boss in scene 3).</summary>
    Victory,
}
