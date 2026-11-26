using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Named controller button layout entry stored inside a ControllerPresetCatalog.
/// </summary>
[Serializable]
public class ControllerPreset
{
    public string id = ControllerPresetCatalog.DefaultPresetId;
    public string displayName = "Classic (GDD)";
    [TextArea] public string description = string.Empty;
    public List<ControllerActionBinding> bindings = new List<ControllerActionBinding>();

    public bool TryGetBinding(string actionName, out ControllerActionBinding match)
    {
        if (!string.IsNullOrWhiteSpace(actionName) && bindings != null)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                ControllerActionBinding candidate = bindings[i];
                if (string.Equals(candidate.actionName, actionName, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(candidate.bindingPath))
                {
                    match = candidate;
                    return true;
                }
            }
        }

        match = default;
        return false;
    }

    public string GetBindingPath(string actionName)
    {
        return TryGetBinding(actionName, out ControllerActionBinding match) ? match.bindingPath : string.Empty;
    }

    public static ControllerPreset CreateClassic()
    {
        return new ControllerPreset
        {
            id = ControllerPresetCatalog.DefaultPresetId,
            displayName = "Classic (GDD)",
            description = "Standard GDD face-button layout.",
            bindings = new List<ControllerActionBinding>
            {
                new ControllerActionBinding("Attack", "<Gamepad>/buttonSouth", "Normal Attack"),
                new ControllerActionBinding("Special", "<Gamepad>/buttonNorth", "Special Attack"),
                new ControllerActionBinding("Dash", "<Gamepad>/buttonEast", "Dash"),
                new ControllerActionBinding("Interact", "<Gamepad>/buttonWest", "Interact"),
                new ControllerActionBinding("Pause", "<Gamepad>/start", "Pause"),
            }
        };
    }

    public static ControllerPreset CreateAction()
    {
        return new ControllerPreset
        {
            id = ControllerPresetCatalog.ActionPresetId,
            displayName = "Action (Shoulders)",
            description = "Shoulder-focused combat layout keeping thumbs on sticks.",
            bindings = new List<ControllerActionBinding>
            {
                new ControllerActionBinding("Attack", "<Gamepad>/buttonWest", "Normal Attack"),
                new ControllerActionBinding("Special", "<Gamepad>/rightTrigger", "Special Attack"),
                new ControllerActionBinding("Dash", "<Gamepad>/rightShoulder", "Dash"),
                new ControllerActionBinding("Interact", "<Gamepad>/buttonSouth", "Interact"),
                new ControllerActionBinding("Pause", "<Gamepad>/start", "Pause"),
            }
        };
    }
}
