using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// EditMode tests for GDD controller bindings, ControllerPresetCatalog lookup and fallbacks,
/// ControllerRebindService preset/per-key overrides, InputDeviceTracker family detection,
/// and SoundManager audio consolidation.
/// </summary>
public class ControllerLayoutTests
{
    private const string CatalogAssetPath = "Assets/_Game/Data/Settings/ControllerPresetCatalog.asset";
    private const string InputActionsAssetPath = "Assets/_Game/Settings/InputSystem_Actions.inputactions";
    private const float Tolerance = 0.0001f;

    private TempSaveFolder tempFolder;
    private SettingsService settingsService;
    private InputActionAsset runtimeActions;
    private ControllerPresetCatalog runtimeCatalog;
    private GameObject testHost;

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
        if (testHost != null)
            UnityEngine.Object.DestroyImmediate(testHost);

        if (runtimeActions != null)
            UnityEngine.Object.DestroyImmediate(runtimeActions);

        if (runtimeCatalog != null)
            UnityEngine.Object.DestroyImmediate(runtimeCatalog);

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
}
