using System;

/// <summary>
/// The title screen as the menu logic sees it (GDD > Menus > Main Menu: Resume, Settings,
/// Exit). Any component can be the view - today a Canvas, later an in-world object.
/// </summary>
public interface IMainMenuView : IMenuView
{
    event Action ResumeRequested;
    event Action SettingsRequested;
    event Action ExitRequested;
}
