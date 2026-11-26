using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Applies ControllerPreset layouts and interactive per-key gamepad rebindings to an
/// InputActionAsset while keeping all keyboard and mouse bindings untouched. Persists active
/// preset IDs and custom binding overrides JSON through SettingsService.
/// </summary>
public class ControllerRebindService : IDisposable
{
    private readonly InputActionAsset actionAsset;
    private readonly ControllerPresetCatalog catalog;
    private readonly SettingsService settingsService;

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    public InputActionAsset ActionAsset => actionAsset;
    public ControllerPresetCatalog Catalog => catalog;
    public string ActivePresetId { get; private set; } = ControllerPresetCatalog.DefaultPresetId;
    public bool IsRebinding => rebindingOperation != null;

    public event Action<string> BindingsChanged;
    public event Action<string> RebindStarted;
    public event Action<string, string> RebindCompleted;
    public event Action<string> RebindCanceled;

    public ControllerRebindService(
        InputActionAsset actionAsset,
        ControllerPresetCatalog catalog = null,
        SettingsService settingsService = null)
    {
        this.actionAsset = actionAsset;
        this.catalog = catalog;
        this.settingsService = settingsService;
    }

    public void Dispose()
    {
        CancelInteractiveRebind();
    }

    /// <summary>Loads and applies the controller preset or custom overrides stored in SettingsService.</summary>
    public void LoadSavedBindings()
    {
        ApplyFromSettings(settingsService != null ? settingsService.Data : null);
    }

    /// <summary>Applies a SettingsData snapshot without marking SettingsService dirty.</summary>
    public void ApplyFromSettings(SettingsData settings)
    {
        if (actionAsset == null)
            return;

        CancelInteractiveRebind();

        string requestedPresetId = settings != null
            ? SettingsData.NormalizePresetId(settings.controllerPresetId)
            : ControllerPresetCatalog.DefaultPresetId;

        if (ControllerPresetCatalog.IsCustomPreset(requestedPresetId))
        {
            ControllerPreset basePreset = ResolvePreset(ControllerPresetCatalog.DefaultPresetId);
            ApplyPresetInternal(basePreset);

            string overridesJson = settings != null ? settings.bindingOverridesJson : string.Empty;
            if (!string.IsNullOrWhiteSpace(overridesJson))
            {
                try
                {
                    actionAsset.LoadBindingOverridesFromJson(overridesJson, removeExisting: false);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ControllerRebindService] Invalid bindingOverridesJson, falling back to default preset: {e.Message}");
                    ApplyPresetInternal(basePreset);
                    ActivePresetId = basePreset.id;
                    BindingsChanged?.Invoke(ActivePresetId);
                    return;
                }
            }

            ActivePresetId = ControllerPresetCatalog.CustomPresetId;
            BindingsChanged?.Invoke(ActivePresetId);
            return;
        }

        ControllerPreset preset = ResolvePreset(requestedPresetId);
        ApplyPresetInternal(preset);
        ActivePresetId = preset.id;
        BindingsChanged?.Invoke(ActivePresetId);
    }

    /// <summary>Applies a preset by ID (falling back to 'classic' if missing) and optionally updates SettingsService.</summary>
    public bool ApplyPreset(string presetId, bool persistToSettings = true)
    {
        if (actionAsset == null)
            return false;

        if (ControllerPresetCatalog.IsCustomPreset(presetId))
        {
            ActivePresetId = ControllerPresetCatalog.CustomPresetId;
            string overridesJson = actionAsset.SaveBindingOverridesAsJson();
            if (persistToSettings)
                settingsService?.SetControllerPreset(ControllerPresetCatalog.CustomPresetId, overridesJson);

            BindingsChanged?.Invoke(ActivePresetId);
            return true;
        }

        ControllerPreset preset = ResolvePreset(presetId);
        return ApplyPreset(preset, persistToSettings);
    }

    /// <summary>Applies all gamepad bindings in a ControllerPreset while leaving keyboard/mouse bindings intact.</summary>
    public bool ApplyPreset(ControllerPreset preset, bool persistToSettings = true)
    {
        if (actionAsset == null || preset == null)
            return false;

        CancelInteractiveRebind();
        ApplyPresetInternal(preset);

        ActivePresetId = SettingsData.NormalizePresetId(preset.id);
        if (persistToSettings)
            settingsService?.SetControllerPreset(ActivePresetId, string.Empty);

        BindingsChanged?.Invoke(ActivePresetId);
        return true;
    }

    /// <summary>
    /// Overrides a single action's gamepad binding, automatically switches ActivePresetId to 'custom',
    /// and persists SaveBindingOverridesAsJson() into SettingsService.
    /// </summary>
    public bool ApplyActionOverride(string actionName, string gamepadBindingPath, bool persistToSettings = true)
    {
        if (actionAsset == null
            || string.IsNullOrWhiteSpace(actionName)
            || string.IsNullOrWhiteSpace(gamepadBindingPath))
        {
            return false;
        }

        CancelInteractiveRebind();

        InputAction action = FindAction(actionAsset, actionName);
        if (action == null)
            return false;

        int bindingIndex = FindGamepadBindingIndex(action);
        if (bindingIndex < 0)
            return false;

        action.ApplyBindingOverride(bindingIndex, gamepadBindingPath.Trim());
        ActivePresetId = ControllerPresetCatalog.CustomPresetId;

        string overridesJson = actionAsset.SaveBindingOverridesAsJson();
        if (persistToSettings)
            settingsService?.SetControllerPreset(ControllerPresetCatalog.CustomPresetId, overridesJson);

        BindingsChanged?.Invoke(ActivePresetId);
        return true;
    }

    /// <summary>
    /// Starts an interactive key-by-key gamepad rebinding for a single action, filtering to &lt;Gamepad&gt;
    /// controls, excluding sticks and cancel controls, and switching the active preset to 'custom' on completion.
    /// </summary>
    public bool StartInteractiveRebind(
        string actionName,
        Action<string> onCompleted = null,
        Action onCanceled = null,
        string cancelBindingPath = "<Keyboard>/escape")
    {
        if (actionAsset == null || string.IsNullOrWhiteSpace(actionName))
            return false;

        CancelInteractiveRebind();

        InputAction action = FindAction(actionAsset, actionName);
        if (action == null)
            return false;

        int bindingIndex = FindGamepadBindingIndex(action);
        if (bindingIndex < 0)
            return false;

        bool wasEnabled = action.enabled;
        if (wasEnabled)
            action.Disable();

        InputActionRebindingExtensions.RebindingOperation operation = action
            .PerformInteractiveRebinding(bindingIndex)
            .WithControlsHavingToMatchPath("<Gamepad>")
            .WithControlsExcluding("<Gamepad>/leftStick")
            .WithControlsExcluding("<Gamepad>/rightStick")
            .WithControlsExcluding("<Gamepad>/select")
            .OnMatchWaitForAnother(0.1f);

        if (!string.IsNullOrWhiteSpace(cancelBindingPath))
            operation.WithCancelingThrough(cancelBindingPath);

        operation.OnComplete(op =>
        {
            string boundPath = action.bindings[bindingIndex].effectivePath;
            op.Dispose();
            rebindingOperation = null;

            if (wasEnabled)
                action.Enable();

            ActivePresetId = ControllerPresetCatalog.CustomPresetId;
            string overridesJson = actionAsset.SaveBindingOverridesAsJson();
            settingsService?.SetControllerPreset(ControllerPresetCatalog.CustomPresetId, overridesJson);

            RebindCompleted?.Invoke(actionName, boundPath);
            BindingsChanged?.Invoke(ActivePresetId);
            onCompleted?.Invoke(boundPath);
        });

        operation.OnCancel(op =>
        {
            op.Dispose();
            rebindingOperation = null;

            if (wasEnabled)
                action.Enable();

            RebindCanceled?.Invoke(actionName);
            onCanceled?.Invoke();
        });

        rebindingOperation = operation;
        RebindStarted?.Invoke(actionName);
        rebindingOperation.Start();
        return true;
    }

    public void CancelInteractiveRebind()
    {
        if (rebindingOperation == null)
            return;

        InputActionRebindingExtensions.RebindingOperation active = rebindingOperation;
        rebindingOperation = null;
        active.Cancel();
        active.Dispose();
    }

    public bool ResetToDefaultPreset(bool persistToSettings = true)
    {
        return ApplyPreset(ControllerPresetCatalog.DefaultPresetId, persistToSettings);
    }

    public string GetEffectiveGamepadPath(string actionName)
    {
        InputAction action = FindAction(actionAsset, actionName);
        if (action == null)
            return string.Empty;

        int bindingIndex = FindGamepadBindingIndex(action);
        return bindingIndex >= 0 ? action.bindings[bindingIndex].effectivePath : string.Empty;
    }

    public List<string> GetKeyboardMousePaths(string actionName)
    {
        var paths = new List<string>();
        InputAction action = FindAction(actionAsset, actionName);
        if (action == null)
            return paths;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || IsGamepadBinding(binding))
                continue;

            string effective = binding.effectivePath;
            if (!string.IsNullOrWhiteSpace(effective)
                && (effective.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)
                    || effective.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase)))
            {
                paths.Add(effective);
            }
        }

        return paths;
    }

    private void ApplyPresetInternal(ControllerPreset preset)
    {
        ClearGamepadBindingOverrides(actionAsset);
        if (preset == null || preset.bindings == null)
            return;

        for (int i = 0; i < preset.bindings.Count; i++)
        {
            ControllerActionBinding entry = preset.bindings[i];
            if (!entry.IsValid)
                continue;

            InputAction action = FindAction(actionAsset, entry.actionName);
            if (action == null)
                continue;

            int bindingIndex = FindGamepadBindingIndex(action);
            if (bindingIndex >= 0)
                action.ApplyBindingOverride(bindingIndex, entry.bindingPath.Trim());
        }
    }

    private ControllerPreset ResolvePreset(string presetId)
    {
        if (catalog != null)
            return catalog.GetPresetOrFallback(presetId);

        if (string.Equals(presetId, ControllerPresetCatalog.ActionPresetId, StringComparison.OrdinalIgnoreCase))
            return ControllerPreset.CreateAction();

        return ControllerPreset.CreateClassic();
    }

    public static void ClearGamepadBindingOverrides(InputActionAsset asset)
    {
        if (asset == null)
            return;

        foreach (InputActionMap map in asset.actionMaps)
        {
            foreach (InputAction action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (IsGamepadBinding(action.bindings[i]))
                        action.RemoveBindingOverride(i);
                }
            }
        }
    }

    public static InputAction FindAction(InputActionAsset asset, string actionName)
    {
        if (asset == null || string.IsNullOrWhiteSpace(actionName))
            return null;

        string trimmed = actionName.Trim();
        InputAction direct = asset.FindAction(trimmed, false);
        if (direct != null)
            return direct;

        foreach (InputActionMap map in asset.actionMaps)
        {
            foreach (InputAction action in map.actions)
            {
                if (string.Equals(action.name, trimmed, StringComparison.OrdinalIgnoreCase))
                    return action;
            }
        }

        return null;
    }

    public static int FindGamepadBindingIndex(InputAction action)
    {
        if (action == null)
            return -1;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            if (IsGamepadBinding(binding))
                return i;
        }

        return -1;
    }

    public static bool IsGamepadBinding(InputBinding binding)
    {
        if (binding.isComposite)
            return false;

        if (!string.IsNullOrEmpty(binding.groups)
            && binding.groups.IndexOf("Gamepad", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        string path = !string.IsNullOrEmpty(binding.path) ? binding.path : binding.effectivePath;
        return !string.IsNullOrEmpty(path)
               && path.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase);
    }
}
