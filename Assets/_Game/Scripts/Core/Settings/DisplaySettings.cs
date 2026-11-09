using UnityEngine;

/// <summary>
/// The Video options the Settings Menu offers (GDD > Menus > Settings Menu): its resolution
/// list (16:9 then 16:10) and the three window modes, plus applying them to the screen.
/// </summary>
public static class DisplaySettings
{
    public static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(2560, 1440),
        new Vector2Int(1920, 1080),
        new Vector2Int(1366, 768),
        new Vector2Int(1280, 720),
        new Vector2Int(1920, 1200),
        new Vector2Int(1680, 1050),
        new Vector2Int(1440, 900),
        new Vector2Int(1280, 800),
    };

    /// <summary>Fullscreen, borderless, windowed - in the order the dropdown shows them.</summary>
    public static readonly FullScreenMode[] Modes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed,
    };

    public static string ResolutionLabel(int index)
    {
        Vector2Int size = Resolutions[index];
        string aspect = size.x * 10 == size.y * 16 ? "16:10" : "16:9";
        return $"{size.x} x {size.y} ({aspect})";
    }

    public static string ModeLabel(int index)
    {
        switch (Modes[index])
        {
            case FullScreenMode.ExclusiveFullScreen: return "Fullscreen";
            case FullScreenMode.FullScreenWindow: return "Borderless";
            default: return "Windowed";
        }
    }

    public static int IndexOfResolution(int width, int height)
    {
        for (int i = 0; i < Resolutions.Length; i++)
        {
            if (Resolutions[i].x == width && Resolutions[i].y == height) return i;
        }

        return -1;
    }

    /// <summary>The listed resolution with the most similar pixel count (used when "native" isn't in the list).</summary>
    public static int ClosestResolution(int width, int height)
    {
        int best = 0;
        long bestDistance = long.MaxValue;
        for (int i = 0; i < Resolutions.Length; i++)
        {
            long distance = System.Math.Abs((long)Resolutions[i].x * Resolutions[i].y - (long)width * height);
            if (distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    public static int IndexOfMode(FullScreenMode mode) => System.Array.IndexOf(Modes, mode);

    public static void Apply(SettingsData settings)
    {
        // The editor's Game view ignores these calls; only builds have a window to resize.
        if (Application.isEditor || settings == null) return;

        int width = settings.resolutionWidth > 0 ? settings.resolutionWidth : Screen.currentResolution.width;
        int height = settings.resolutionHeight > 0 ? settings.resolutionHeight : Screen.currentResolution.height;
        Screen.SetResolution(width, height, settings.FullscreenMode);
    }
}
