# Scripts / UI
UIManager.
UI prefab: Prefabs/UI/UI.prefab. Sprites: Art/UI.
Owner: Ivan.
SettingsCanvasView draws the settings book, manages gamepad navigation, and opens/closes it. KeybindsPanel auto-detects Keyboard/Mouse vs Gamepad via InputDeviceTracker, switches between KeyboardMouseDiagramView and ControllerVisualizerView, cycles modular ControllerPresetCatalog presets, and runs interactive per-action gamepad rebinding. UIManager.OpenSettings and UIManager.ReturnToMainMenu wire the pause menu's Options and Main Menu buttons.
