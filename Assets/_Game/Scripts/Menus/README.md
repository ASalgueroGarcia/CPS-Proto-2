# Scripts / Menus
Menu logic: the presenters (title screen, save slots, settings), the view interfaces they talk to
(`IMainMenuView`, `ISlotSelectView`, `ISettingsView`, `IConfirmDialog`), `SlotCardText` and `MainMenuInstaller`.
Not here: anything you see. Views live in `UI/` (Canvas) or, later, wherever an in-world (diegetic) menu lives:
a view only implements an interface, so swapping one never touches this folder.
Owner: Dima + Ivan. Uses only Core (`Spectracle.Menus`); UI and the tests use it.
