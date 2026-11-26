using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// EditMode tests for GDD controller bindings, ControllerPresetCatalog lookup and fallbacks,
/// ControllerRebindService preset/per-key overrides, InputDeviceTracker family detection,
/// live ControllerVisualizerView highlight mapping, KeybindsPanel preset cycling,
/// pause menu button flow, and SoundManager audio consolidation.
/// </summary>
public class ControllerLayoutTests
{
    private const string CatalogAssetPath = "Assets/_Game/Data/Settings/ControllerPresetCatalog.asset";
    private const string InputActionsAssetPath = "Assets/_Game/Settings/InputSystem_Actions.inputactions";
    private const string SettingsMenuPrefabPath = "Assets/_Game/Prefabs/UI/SettingsMenu.prefab";
    private const string UiPrefabPath = "Assets/_Game/Prefabs/UI/UI.prefab";
    private const float Tolerance = 0.0001f;

    private TempSaveFolder tempFolder;
    private SettingsService settingsService;
    private InputActionAsset runtimeActions;
    private ControllerPresetCatalog runtimeCatalog;
    private GameObject testHost;
    private GameObject spawnedPrefab;

    [SetUp]
    public void SetUp()
    {
        tempFolder = new TempSaveFolder();
        settingsService = new SettingsService(tempFolder.Store);
        settingsService.Load();

        InputActionAsset sourceActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath);
        Assert.That(sourceActions, Is.Not.Null, $"Missing {InputActionsAssetPath}");
        runtimeActions = UnityEngine.Object.Instantiate(sourceActions);

        ControllerPresetCatalog sourceCatalog = AssetDatabase.LoadAssetAtPath<ControllerPresetCatalog>(CatalogAssetPath);
        Assert.That(sourceCatalog, Is.Not.Null, $"Missing {CatalogAssetPath}");
        runtimeCatalog = UnityEngine.Object.Instantiate(sourceCatalog);
    }

    [TearDown]
    public void TearDown()
    {
        if (spawnedPrefab != null)
            UnityEngine.Object.DestroyImmediate(spawnedPrefab);

        if (testHost != null)
            UnityEngine.Object.DestroyImmediate(testHost);

        if (InputDeviceTracker.Instance != null)
            UnityEngine.Object.DestroyImmediate(InputDeviceTracker.Instance.gameObject);

        KeybindsPanel.ResetSharedServiceForTests();

        if (runtimeActions != null)
            UnityEngine.Object.DestroyImmediate(runtimeActions);

        if (runtimeCatalog != null)
            UnityEngine.Object.DestroyImmediate(runtimeCatalog);

        Time.timeScale = 1f;
        Services.ResetForTests();
        tempFolder?.Dispose();
    }

    [Test]
    public void Catalog_asset_contains_classic_and_action_presets_matching_spec()
    {
        Assert.That(runtimeCatalog.Presets.Count, Is.EqualTo(2));

        Assert.That(runtimeCatalog.TryGetPreset("classic", out ControllerPreset classic), Is.True);
        Assert.That(classic.displayName, Is.EqualTo("Classic (GDD)"));
        Assert.That(classic.GetBindingPath("Attack"), Is.EqualTo("<Gamepad>/buttonSouth"));
        Assert.That(classic.GetBindingPath("Special"), Is.EqualTo("<Gamepad>/buttonNorth"));
        Assert.That(classic.GetBindingPath("Dash"), Is.EqualTo("<Gamepad>/buttonEast"));
        Assert.That(classic.GetBindingPath("Interact"), Is.EqualTo("<Gamepad>/buttonWest"));
        Assert.That(classic.GetBindingPath("Pause"), Is.EqualTo("<Gamepad>/start"));

        Assert.That(runtimeCatalog.TryGetPreset("action", out ControllerPreset action), Is.True);
        Assert.That(action.displayName, Is.EqualTo("Action (Shoulders)"));
        Assert.That(action.GetBindingPath("Attack"), Is.EqualTo("<Gamepad>/buttonWest"));
        Assert.That(action.GetBindingPath("Special"), Is.EqualTo("<Gamepad>/rightTrigger"));
        Assert.That(action.GetBindingPath("Dash"), Is.EqualTo("<Gamepad>/rightShoulder"));
        Assert.That(action.GetBindingPath("Interact"), Is.EqualTo("<Gamepad>/buttonSouth"));
        Assert.That(action.GetBindingPath("Pause"), Is.EqualTo("<Gamepad>/start"));
    }

    [Test]
    public void Input_actions_asset_matches_gdd_gamepad_and_keyboard_bindings()
    {
        using var service = new ControllerRebindService(runtimeActions, runtimeCatalog, settingsService);

        InputAction move = ControllerRebindService.FindAction(runtimeActions, "Move");
        Assert.That(move, Is.Not.Null);
        Assert.That(ControllerRebindService.FindGamepadBindingIndex(move), Is.GreaterThanOrEqualTo(0));
        Assert.That(service.GetEffectiveGamepadPath("Move"), Is.EqualTo("<Gamepad>/leftStick"));

        Assert.That(service.GetEffectiveGamepadPath("Attack"), Is.EqualTo("<Gamepad>/buttonSouth"));
        CollectionAssert.Contains(service.GetKeyboardMousePaths("Attack"), "<Mouse>/leftButton");

        Assert.That(service.GetEffectiveGamepadPath("Special"), Is.EqualTo("<Gamepad>/buttonNorth"));
        CollectionAssert.Contains(service.GetKeyboardMousePaths("Special"), "<Mouse>/rightButton");

        Assert.That(service.GetEffectiveGamepadPath("Dash"), Is.EqualTo("<Gamepad>/buttonEast"));
        List<string> dashKeys = service.GetKeyboardMousePaths("Dash");
        CollectionAssert.Contains(dashKeys, "<Keyboard>/space");
        CollectionAssert.Contains(dashKeys, "<Keyboard>/shift");

        Assert.That(service.GetEffectiveGamepadPath("Interact"), Is.EqualTo("<Gamepad>/buttonWest"));
        CollectionAssert.Contains(service.GetKeyboardMousePaths("Interact"), "<Keyboard>/e");

        Assert.That(service.GetEffectiveGamepadPath("PAUSE"), Is.EqualTo("<Gamepad>/start"));
        CollectionAssert.Contains(service.GetKeyboardMousePaths("PAUSE"), "<Keyboard>/escape");
    }

    [Test]
    public void Catalog_handles_duplicate_and_missing_presets_with_safe_fallback()
    {
        ControllerPresetCatalog scratchCatalog = ScriptableObject.CreateInstance<ControllerPresetCatalog>();
        try
        {
            Assert.That(scratchCatalog.HasPreset("nonexistent"), Is.False);
            ControllerPreset emptyFallback = scratchCatalog.GetPresetOrFallback("nonexistent");
            Assert.That(emptyFallback.id, Is.EqualTo("classic"));
            Assert.That(emptyFallback.GetBindingPath("Attack"), Is.EqualTo("<Gamepad>/buttonSouth"));

            var firstDup = new ControllerPreset
            {
                id = "dup",
                displayName = "First Duplicate",
                bindings = new List<ControllerActionBinding>
                {
                    new ControllerActionBinding("Attack", "<Gamepad>/leftShoulder")
                }
            };
            var secondDup = new ControllerPreset
            {
                id = "dup",
                displayName = "Second Duplicate",
                bindings = new List<ControllerActionBinding>
                {
                    new ControllerActionBinding("Attack", "<Gamepad>/rightShoulder")
                }
            };

            scratchCatalog.SetPresets(new[] { firstDup, secondDup, ControllerPreset.CreateClassic() });

            Assert.That(scratchCatalog.TryGetPreset("DUP", out ControllerPreset resolvedDup), Is.True);
            Assert.That(resolvedDup.displayName, Is.EqualTo("First Duplicate"));
            Assert.That(resolvedDup.GetBindingPath("Attack"), Is.EqualTo("<Gamepad>/leftShoulder"));

            ControllerPreset missingFallback = scratchCatalog.GetPresetOrFallback("deleted_preset");
            Assert.That(missingFallback.id, Is.EqualTo("classic"));
            Assert.That(scratchCatalog.ResolveActivePresetId("custom"), Is.EqualTo("custom"));
            Assert.That(scratchCatalog.ResolveActivePresetId("deleted_preset"), Is.EqualTo("classic"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(scratchCatalog);
        }
    }

    [Test]
    public void Catalog_resolves_per_action_overrides_when_preset_is_custom()
    {
        var overrides = new List<ControllerActionBinding>
        {
            new ControllerActionBinding("Attack", "<Gamepad>/leftTrigger"),
            new ControllerActionBinding("Dash", "<Gamepad>/leftShoulder")
        };

        Assert.That(runtimeCatalog.ResolveBindingPath("custom", "Attack", overrides), Is.EqualTo("<Gamepad>/leftTrigger"));
        Assert.That(runtimeCatalog.ResolveBindingPath("custom", "Dash", overrides), Is.EqualTo("<Gamepad>/leftShoulder"));
        Assert.That(runtimeCatalog.ResolveBindingPath("custom", "Special", overrides), Is.EqualTo("<Gamepad>/buttonNorth"));
        Assert.That(runtimeCatalog.ResolveBindingPath("action", "Attack", overrides), Is.EqualTo("<Gamepad>/buttonWest"));
    }

    [Test]
    public void Rebind_service_switches_presets_applies_custom_overrides_and_reloads_on_startup()
    {
        using var service = new ControllerRebindService(runtimeActions, runtimeCatalog, settingsService);

        Assert.That(service.ApplyPreset("action"), Is.True);
        Assert.That(service.ActivePresetId, Is.EqualTo("action"));
        Assert.That(settingsService.Data.controllerPresetId, Is.EqualTo("action"));
        Assert.That(service.GetEffectiveGamepadPath("Attack"), Is.EqualTo("<Gamepad>/buttonWest"));
        Assert.That(service.GetEffectiveGamepadPath("Special"), Is.EqualTo("<Gamepad>/rightTrigger"));
        Assert.That(service.GetEffectiveGamepadPath("Dash"), Is.EqualTo("<Gamepad>/rightShoulder"));
        Assert.That(service.GetEffectiveGamepadPath("Interact"), Is.EqualTo("<Gamepad>/buttonSouth"));

        // Keyboard and mouse bindings must remain untouched after applying controller presets.
        CollectionAssert.Contains(service.GetKeyboardMousePaths("Attack"), "<Mouse>/leftButton");
        CollectionAssert.Contains(service.GetKeyboardMousePaths("Special"), "<Mouse>/rightButton");
        CollectionAssert.Contains(service.GetKeyboardMousePaths("Dash"), "<Keyboard>/space");
        CollectionAssert.Contains(service.GetKeyboardMousePaths("Dash"), "<Keyboard>/shift");
        CollectionAssert.Contains(service.GetKeyboardMousePaths("Interact"), "<Keyboard>/e");

        // Per-key override automatically switches active preset to 'custom' and stores overrides JSON.
        Assert.That(service.ApplyActionOverride("Attack", "<Gamepad>/leftShoulder"), Is.True);
        Assert.That(service.ActivePresetId, Is.EqualTo(ControllerPresetCatalog.CustomPresetId));
        Assert.That(settingsService.Data.controllerPresetId, Is.EqualTo(ControllerPresetCatalog.CustomPresetId));
        Assert.That(settingsService.Data.bindingOverridesJson, Is.Not.Empty);
        Assert.That(service.GetEffectiveGamepadPath("Attack"), Is.EqualTo("<Gamepad>/leftShoulder"));
        Assert.That(service.GetEffectiveGamepadPath("Dash"), Is.EqualTo("<Gamepad>/rightShoulder"));
        Assert.That(settingsService.Save(), Is.True);

        // Simulate a fresh game startup loading saved overrides onto a clean InputActionAsset copy.
        InputActionAsset freshActions = UnityEngine.Object.Instantiate(
            AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsAssetPath));
        try
        {
            var reloadedSettings = new SettingsService(tempFolder.Store);
            reloadedSettings.Load();

            using var startupService = new ControllerRebindService(freshActions, runtimeCatalog, reloadedSettings);
            startupService.LoadSavedBindings();

            Assert.That(startupService.ActivePresetId, Is.EqualTo(ControllerPresetCatalog.CustomPresetId));
            Assert.That(startupService.GetEffectiveGamepadPath("Attack"), Is.EqualTo("<Gamepad>/leftShoulder"));
            Assert.That(startupService.GetEffectiveGamepadPath("Dash"), Is.EqualTo("<Gamepad>/rightShoulder"));
            CollectionAssert.Contains(startupService.GetKeyboardMousePaths("Attack"), "<Mouse>/leftButton");

            // Switching back to 'classic' clears custom overrides and restores GDD defaults.
            Assert.That(startupService.ApplyPreset("classic"), Is.True);
            Assert.That(startupService.ActivePresetId, Is.EqualTo("classic"));
            Assert.That(reloadedSettings.Data.controllerPresetId, Is.EqualTo("classic"));
            Assert.That(reloadedSettings.Data.bindingOverridesJson, Is.EqualTo(string.Empty));
            Assert.That(startupService.GetEffectiveGamepadPath("Attack"), Is.EqualTo("<Gamepad>/buttonSouth"));
            Assert.That(startupService.GetEffectiveGamepadPath("Dash"), Is.EqualTo("<Gamepad>/buttonEast"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(freshActions);
        }
    }

    [Test]
    public void Input_device_tracker_identifies_families_and_emits_device_changes_cleanly()
    {
        Assert.That(InputDeviceTracker.DetectGamepadFamily("DualSenseGamepadHID", "DualSense Wireless Controller", "Sony Interactive Entertainment"),
            Is.EqualTo(GamepadFamily.PlayStation));
        Assert.That(InputDeviceTracker.DetectGamepadFamily("DualShock4GamepadHID", "Wireless Controller", "Sony"),
            Is.EqualTo(GamepadFamily.PlayStation));
        Assert.That(InputDeviceTracker.DetectGamepadFamily("XInputControllerWindows", "Xbox Controller", "Microsoft"),
            Is.EqualTo(GamepadFamily.Xbox));
        Assert.That(InputDeviceTracker.DetectGamepadFamily("HID", "8BitDo Controller", "8BitDo"),
            Is.EqualTo(GamepadFamily.Generic));

        Assert.That(InputDeviceTracker.FormatGamepadBindingLabel("<Gamepad>/buttonSouth", GamepadFamily.PlayStation), Is.EqualTo("Cross"));
        Assert.That(InputDeviceTracker.FormatGamepadBindingLabel("<Gamepad>/buttonSouth", GamepadFamily.Xbox), Is.EqualTo("A"));
        Assert.That(InputDeviceTracker.FormatGamepadBindingLabel("<Gamepad>/rightTrigger", GamepadFamily.PlayStation), Is.EqualTo("R2"));
        Assert.That(InputDeviceTracker.FormatGamepadBindingLabel("<Gamepad>/rightTrigger", GamepadFamily.Xbox), Is.EqualTo("RT"));

        testHost = new GameObject("TrackerTestHost");
        InputDeviceTracker tracker = testHost.AddComponent<InputDeviceTracker>();

        int deviceEvents = 0;
        int familyEvents = 0;
        tracker.DeviceChanged += _ => deviceEvents++;
        tracker.GamepadFamilyChanged += _ => familyEvents++;

        tracker.SetActiveDevice(InputDeviceKind.Gamepad, GamepadFamily.PlayStation);
        Assert.That(tracker.CurrentDevice, Is.EqualTo(InputDeviceKind.Gamepad));
        Assert.That(tracker.CurrentGamepadFamily, Is.EqualTo(GamepadFamily.PlayStation));
        Assert.That(tracker.IsUsingGamepad, Is.True);
        Assert.That(deviceEvents, Is.EqualTo(1));
        Assert.That(familyEvents, Is.EqualTo(1));

        // Repeating the identical device and family does not spam events.
        tracker.SetActiveDevice(InputDeviceKind.Gamepad, GamepadFamily.PlayStation);
        Assert.That(deviceEvents, Is.EqualTo(1));
        Assert.That(familyEvents, Is.EqualTo(1));

        // Returning to keyboard/mouse fires DeviceChanged once.
        tracker.SetActiveDevice(InputDeviceKind.KeyboardMouse, tracker.CurrentGamepadFamily);
        Assert.That(tracker.CurrentDevice, Is.EqualTo(InputDeviceKind.KeyboardMouse));
        Assert.That(tracker.IsUsingGamepad, Is.False);
        Assert.That(deviceEvents, Is.EqualTo(2));
    }

    [Test]
    public void Sound_manager_consolidates_runtime_pitch_speed_pause_and_menu_distortion()
    {
        testHost = new GameObject("SoundManagerTestHost");
        SoundManager sound = testHost.AddComponent<SoundManager>();

        sound.SetMusicPitch(1.5f, 0f);
        sound.SetMusicSpeed(0.75f, 0f);
        sound.SetSfxPitch(1.25f, 0f);
        sound.TickAudio(0f);

        Assert.That(sound.MusicPitch, Is.EqualTo(1.5f).Within(Tolerance));
        Assert.That(sound.MusicSpeed, Is.EqualTo(0.75f).Within(Tolerance));
        Assert.That(sound.SfxPitch, Is.EqualTo(1.25f).Within(Tolerance));
        Assert.That(AudioPitch.ShifterRatio(sound.MusicPitch, sound.MusicSpeed), Is.EqualTo(2f).Within(Tolerance));

        // Values outside AudioTuning limits clamp cleanly, then reset back to 1.
        sound.SetMusicPitch(9f, 0f);
        sound.SetMusicSpeed(0.1f, 0f);
        sound.TickAudio(0f);
        Assert.That(sound.MusicPitch, Is.EqualTo(2f).Within(Tolerance));
        Assert.That(sound.MusicSpeed, Is.EqualTo(0.5f).Within(Tolerance));

        sound.ResetPitchAndSpeed(0f);
        sound.TickAudio(0f);
        Assert.That(sound.MusicPitch, Is.EqualTo(1f).Within(Tolerance));
        Assert.That(sound.MusicSpeed, Is.EqualTo(1f).Within(Tolerance));
        Assert.That(sound.SfxPitch, Is.EqualTo(1f).Within(Tolerance));

        // Settings menu distortion closes and reopens the filter cleanly.
        Assert.That(sound.CurrentLowpassHz, Is.EqualTo(AudioFilter.OpenLowpassHz).Within(0.1f));
        sound.SetMenuDistortion(true);
        sound.TickAudio(1f);
        Assert.That(sound.IsMenuDistortionActive, Is.True);
        Assert.That(sound.CurrentLowpassHz, Is.EqualTo(2400f).Within(0.1f));
        Assert.That(sound.CurrentHighpassHz, Is.EqualTo(320f).Within(0.1f));

        // Opening and closing Settings inside Pause preserves the active pause muffle.
        sound.SetPauseNoise(true);
        sound.TickAudio(1f);
        Assert.That(sound.IsPauseNoiseActive, Is.True);
        Assert.That(sound.CurrentLowpassHz, Is.EqualTo(1800f).Within(0.1f));

        sound.SetMenuDistortion(false);
        sound.TickAudio(1f);
        Assert.That(sound.IsMenuDistortionActive, Is.False);
        Assert.That(sound.IsPauseNoiseActive, Is.True);
        Assert.That(sound.CurrentLowpassHz, Is.EqualTo(1800f).Within(0.1f));

        sound.SetPauseNoise(false);
        sound.TickAudio(1f);
        Assert.That(sound.IsPauseNoiseActive, Is.False);
        Assert.That(sound.CurrentLowpassHz, Is.EqualTo(AudioFilter.OpenLowpassHz).Within(0.1f));
        Assert.That(sound.CurrentHighpassHz, Is.EqualTo(AudioFilter.OpenHighpassHz).Within(0.1f));
    }

    [Test]
    public void Controller_visualizer_maps_bindings_highlights_controls_and_moves_left_stick_nub()
    {
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/leftStick"), Is.EqualTo(ControllerVisualControl.LeftStick));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/dpad/up"), Is.EqualTo(ControllerVisualControl.Dpad));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/buttonSouth"), Is.EqualTo(ControllerVisualControl.ButtonSouth));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/buttonEast"), Is.EqualTo(ControllerVisualControl.ButtonEast));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/buttonWest"), Is.EqualTo(ControllerVisualControl.ButtonWest));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/buttonNorth"), Is.EqualTo(ControllerVisualControl.ButtonNorth));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/leftShoulder"), Is.EqualTo(ControllerVisualControl.LeftShoulder));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/rightShoulder"), Is.EqualTo(ControllerVisualControl.RightShoulder));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/leftTrigger"), Is.EqualTo(ControllerVisualControl.LeftTrigger));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/rightTrigger"), Is.EqualTo(ControllerVisualControl.RightTrigger));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl("<Gamepad>/start"), Is.EqualTo(ControllerVisualControl.Start));
        Assert.That(ControllerVisualizerView.MapBindingPathToControl(string.Empty), Is.EqualTo(ControllerVisualControl.None));

        Assert.That(ControllerVisualizerView.FormatShortFaceGlyph(ControllerVisualControl.ButtonSouth, GamepadFamily.PlayStation), Is.EqualTo("X"));
        Assert.That(ControllerVisualizerView.FormatShortFaceGlyph(ControllerVisualControl.ButtonSouth, GamepadFamily.Xbox), Is.EqualTo("A"));
        Assert.That(ControllerVisualizerView.FormatShortFaceGlyph(ControllerVisualControl.RightTrigger, GamepadFamily.PlayStation), Is.EqualTo("R2"));
        Assert.That(ControllerVisualizerView.FormatShortFaceGlyph(ControllerVisualControl.RightTrigger, GamepadFamily.Xbox), Is.EqualTo("RT"));

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsMenuPrefabPath);
        Assert.That(prefab, Is.Not.Null, $"Missing {SettingsMenuPrefabPath}");
        spawnedPrefab = UnityEngine.Object.Instantiate(prefab);

        ControllerVisualizerView visualizer = spawnedPrefab.GetComponentInChildren<ControllerVisualizerView>(true);
        Assert.That(visualizer, Is.Not.Null);

        using var service = new ControllerRebindService(runtimeActions, runtimeCatalog, settingsService);
        service.ApplyPreset("classic");
        visualizer.RefreshBindings(service, GamepadFamily.PlayStation);

        var input = new ControllerVisualInputState
        {
            leftStick = new Vector2(0.8f, -0.6f),
            buttonSouth = true,
            rightTrigger = 0.85f
        };
        visualizer.ApplyInputState(input);

        Assert.That(visualizer.LeftStickNubOffset.x, Is.GreaterThan(0f));
        Assert.That(visualizer.LeftStickNubOffset.y, Is.LessThan(0f));
        Assert.That(visualizer.IsControlHighlighted(ControllerVisualControl.LeftStick), Is.True);
        Assert.That(visualizer.IsControlHighlighted(ControllerVisualControl.ButtonSouth), Is.True);
        Assert.That(visualizer.IsControlHighlighted(ControllerVisualControl.RightTrigger), Is.True);
        Assert.That(visualizer.IsActionHighlighted("Move"), Is.True);
        Assert.That(visualizer.IsActionHighlighted("Attack"), Is.True);
        Assert.That(visualizer.IsActionHighlighted("Special"), Is.False, "Classic binds Special to North, not Right Trigger.");

        // Switching to Action (Shoulders) moves Special to Right Trigger and Attack to West.
        service.ApplyPreset("action");
        visualizer.RefreshBindings(service, GamepadFamily.Xbox);
        visualizer.ApplyInputState(input);

        Assert.That(visualizer.GetMappedControlForAction("Special"), Is.EqualTo(ControllerVisualControl.RightTrigger));
        Assert.That(visualizer.GetMappedControlForAction("Attack"), Is.EqualTo(ControllerVisualControl.ButtonWest));
        Assert.That(visualizer.IsActionHighlighted("Special"), Is.True);
        Assert.That(visualizer.IsActionHighlighted("Attack"), Is.False);
    }

    [Test]
    public void Keybinds_panel_cycles_modular_presets_transitions_to_custom_and_auto_detects_device()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsMenuPrefabPath);
        Assert.That(prefab, Is.Not.Null, $"Missing {SettingsMenuPrefabPath}");
        spawnedPrefab = UnityEngine.Object.Instantiate(prefab);

        KeybindsPanel panel = spawnedPrefab.GetComponent<KeybindsPanel>();
        Assert.That(panel, Is.Not.Null);

        testHost = new GameObject("KeybindsTrackerHost");
        InputDeviceTracker tracker = testHost.AddComponent<InputDeviceTracker>();

        using var service = new ControllerRebindService(runtimeActions, runtimeCatalog, settingsService);
        panel.Initialize(service, tracker, runtimeCatalog);

        // Starts in Keyboard/Mouse mode until gamepad activity arrives.
        Assert.That(panel.IsGamepadSchemeActive, Is.False);

        // Switching tracker to PlayStation gamepad activates gamepad scheme and glyphs.
        tracker.SetActiveDevice(InputDeviceKind.Gamepad, GamepadFamily.PlayStation);
        Assert.That(panel.IsGamepadSchemeActive, Is.True);
        Assert.That(panel.ActivePresetDisplayName, Is.EqualTo("Classic (GDD)"));

        // Cycling forward moves through catalog presets and wraps around cleanly.
        panel.SelectNextPreset();
        Assert.That(service.ActivePresetId, Is.EqualTo("action"));
        Assert.That(panel.ActivePresetDisplayName, Is.EqualTo("Action (Shoulders)"));

        panel.SelectNextPreset();
        Assert.That(service.ActivePresetId, Is.EqualTo("classic"));
        Assert.That(panel.ActivePresetDisplayName, Is.EqualTo("Classic (GDD)"));

        // Adding a third preset to the catalog updates cycling modularly without code changes.
        var expanded = new List<ControllerPreset>(runtimeCatalog.Presets)
        {
            new ControllerPreset
            {
                id = "southpaw",
                displayName = "Southpaw Custom",
                bindings = new List<ControllerActionBinding>
                {
                    new ControllerActionBinding("Attack", "<Gamepad>/leftTrigger", "Normal Attack")
                }
            }
        };
        runtimeCatalog.SetPresets(expanded);

        panel.SelectPreviousPreset();
        Assert.That(service.ActivePresetId, Is.EqualTo("southpaw"));
        Assert.That(panel.ActivePresetDisplayName, Is.EqualTo("Southpaw Custom"));

        // Per-action rebind override transitions preset label to Custom and updates PlayStation/Xbox glyphs.
        Assert.That(service.ApplyActionOverride("Attack", "<Gamepad>/leftShoulder"), Is.True);
        Assert.That(panel.ActivePresetDisplayName, Is.EqualTo("Custom"));
        Assert.That(FindRow(panel, "Attack").bindingLabel.text, Is.EqualTo("L1"));

        tracker.SetActiveDevice(InputDeviceKind.Gamepad, GamepadFamily.Xbox);
        Assert.That(FindRow(panel, "Attack").bindingLabel.text, Is.EqualTo("LB"));

        // Cycling forward from Custom returns cleanly to the first catalog preset.
        panel.SelectNextPreset();
        Assert.That(service.ActivePresetId, Is.EqualTo("classic"));
        Assert.That(panel.ActivePresetDisplayName, Is.EqualTo("Classic (GDD)"));
    }

    [Test]
    public void Pause_menu_has_only_resume_options_main_menu_and_drives_settings_flow()
    {
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
        Assert.That(uiPrefab, Is.Not.Null, $"Missing {UiPrefabPath}");
        spawnedPrefab = UnityEngine.Object.Instantiate(uiPrefab);

        Transform buttonList = spawnedPrefab.transform.Find("PausePanel/Panel/Signs/ButtonList");
        Assert.That(buttonList, Is.Not.Null, "PausePanel button list is missing.");
        Assert.That(buttonList.childCount, Is.EqualTo(3), "Pause menu must contain only Resume, Options and Main Menu.");
        Assert.That(buttonList.GetChild(0).name, Is.EqualTo("ResumeBtn"));
        Assert.That(buttonList.GetChild(1).name, Is.EqualTo("OptionsBtn"));
        Assert.That(buttonList.GetChild(2).name, Is.EqualTo("MainMenuBtn"));
        Assert.That(buttonList.Find("ReturnToMapBtn"), Is.Null, "Return to Map must not exist in the pause menu.");

        UIManager uiManager = spawnedPrefab.GetComponent<UIManager>();
        Assert.That(uiManager, Is.Not.Null);
        SettingsCanvasView settingsView = spawnedPrefab.GetComponentInChildren<SettingsCanvasView>(true);
        Assert.That(settingsView, Is.Not.Null);

        testHost = new GameObject("PauseTrackerHost");
        InputDeviceTracker tracker = testHost.AddComponent<InputDeviceTracker>();
        using var service = new ControllerRebindService(runtimeActions, runtimeCatalog, settingsService);
        KeybindsPanel keybinds = settingsView.GetComponent<KeybindsPanel>();
        Assert.That(keybinds, Is.Not.Null);
        keybinds.Initialize(service, tracker, runtimeCatalog);

        var save = new SaveService(tempFolder.Store);
        var fakeFlow = new FakePauseFlow();
        Services.Install(save, settingsService, new RunSession(save));
        Services.RegisterFlow(fakeFlow);

        uiManager.Pause();
        Assert.That(uiManager.IsPaused, Is.True);
        Assert.That(uiManager.IsSettingsOpen, Is.False);

        uiManager.OpenSettings();
        Assert.That(uiManager.IsSettingsOpen, Is.True);

        settingsView.Back();
        Assert.That(uiManager.IsSettingsOpen, Is.False);
        Assert.That(uiManager.IsPaused, Is.True, "Closing settings returns to the pause menu.");

        uiManager.ReturnToMainMenu();
        Assert.That(fakeFlow.MainMenuCalls, Is.EqualTo(1));

        uiManager.Resume();
        Assert.That(uiManager.IsPaused, Is.False);
    }

    private static RebindActionRow FindRow(KeybindsPanel panel, string actionName)
    {
        for (int i = 0; i < panel.RebindRows.Count; i++)
        {
            if (string.Equals(panel.RebindRows[i].actionName, actionName, System.StringComparison.OrdinalIgnoreCase))
                return panel.RebindRows[i];
        }

        Assert.Fail($"Rebind row '{actionName}' not found on KeybindsPanel.");
        return null;
    }

    private sealed class FakePauseFlow : IGameFlow
    {
        public int MainMenuCalls { get; private set; }
        public void EnterHub() { }
        public void EnterMainMenu() => MainMenuCalls++;
        public void Quit() { }
    }
}
