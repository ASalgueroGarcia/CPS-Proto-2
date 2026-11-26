using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inspector-editable catalog of controller presets. Designers can add, edit or remove presets
/// in Assets/_Game/Data/Settings/ControllerPresetCatalog.asset without code changes.
/// </summary>
[CreateAssetMenu(fileName = "ControllerPresetCatalog", menuName = "Settings/Controller Preset Catalog")]
public class ControllerPresetCatalog : ScriptableObject
{
    public const string DefaultPresetId = "classic";
    public const string ActionPresetId = "action";
    public const string CustomPresetId = "custom";

    [SerializeField] private List<ControllerPreset> presets = new List<ControllerPreset>();

    public IReadOnlyList<ControllerPreset> Presets => presets;

    public static bool IsCustomPreset(string presetId)
    {
        return !string.IsNullOrWhiteSpace(presetId)
               && string.Equals(presetId.Trim(), CustomPresetId, StringComparison.OrdinalIgnoreCase);
    }

    public void SetPresets(IEnumerable<ControllerPreset> entries)
    {
        presets.Clear();
        if (entries == null) return;

        foreach (ControllerPreset entry in entries)
        {
            if (entry != null)
                presets.Add(entry);
        }
    }

    public bool HasPreset(string presetId)
    {
        return TryGetPreset(presetId, out _);
    }

    /// <summary>
    /// Looks up a preset by ID (case-insensitive). If duplicate IDs exist, the first valid entry wins.
    /// </summary>
    public bool TryGetPreset(string presetId, out ControllerPreset preset)
    {
        if (!string.IsNullOrWhiteSpace(presetId) && presets != null)
        {
            string trimmed = presetId.Trim();
            for (int i = 0; i < presets.Count; i++)
            {
                ControllerPreset candidate = presets[i];
                if (candidate != null
                    && !string.IsNullOrWhiteSpace(candidate.id)
                    && string.Equals(candidate.id.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    preset = candidate;
                    return true;
                }
            }
        }

        preset = null;
        return false;
    }

    /// <summary>
    /// Returns the requested preset, falling back to 'classic', then the first valid catalog entry,
    /// and finally the built-in Classic GDD layout if the catalog is empty.
    /// </summary>
    public ControllerPreset GetPresetOrFallback(string presetId)
    {
        if (TryGetPreset(presetId, out ControllerPreset exact))
            return exact;

        if (TryGetPreset(DefaultPresetId, out ControllerPreset classic))
            return classic;

        if (presets != null)
        {
            for (int i = 0; i < presets.Count; i++)
            {
                if (presets[i] != null && !string.IsNullOrWhiteSpace(presets[i].id))
                    return presets[i];
            }
        }

        return ControllerPreset.CreateClassic();
    }

    /// <summary>
    /// Normalizes a saved preset ID against the catalog while preserving 'custom' for per-key overrides.
    /// </summary>
    public string ResolveActivePresetId(string presetId)
    {
        if (IsCustomPreset(presetId))
            return CustomPresetId;

        if (TryGetPreset(presetId, out ControllerPreset match))
            return match.id;

        return GetPresetOrFallback(DefaultPresetId).id;
    }

    /// <summary>
    /// Resolves the effective binding path for an action, applying per-action custom overrides when
    /// the active preset is 'custom'.
    /// </summary>
    public string ResolveBindingPath(
        string presetId,
        string actionName,
        IReadOnlyList<ControllerActionBinding> customOverrides = null)
    {
        if (string.IsNullOrWhiteSpace(actionName))
            return string.Empty;

        if (IsCustomPreset(presetId) && customOverrides != null)
        {
            for (int i = customOverrides.Count - 1; i >= 0; i--)
            {
                ControllerActionBinding custom = customOverrides[i];
                if (string.Equals(custom.actionName, actionName, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(custom.bindingPath))
                {
                    return custom.bindingPath;
                }
            }
        }

        ControllerPreset basePreset = GetPresetOrFallback(IsCustomPreset(presetId) ? DefaultPresetId : presetId);
        return basePreset.GetBindingPath(actionName);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (presets == null) return;

        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < presets.Count; i++)
        {
            ControllerPreset preset = presets[i];
            if (preset == null || string.IsNullOrWhiteSpace(preset.id))
                continue;

            if (!seenIds.Add(preset.id.Trim()))
            {
                Debug.LogWarning($"[ControllerPresetCatalog] Duplicate preset id '{preset.id}' in {name}.", this);
            }
        }
    }
#endif
}
