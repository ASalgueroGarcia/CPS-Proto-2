using System;

/// <summary>
/// Maps a logical gameplay/UI action name to a controller binding path and optional UI label.
/// </summary>
[Serializable]
public struct ControllerActionBinding
{
    public string actionName;
    public string bindingPath;
    public string displayLabel;

    public ControllerActionBinding(string actionName, string bindingPath, string displayLabel = "")
    {
        this.actionName = actionName ?? string.Empty;
        this.bindingPath = bindingPath ?? string.Empty;
        this.displayLabel = displayLabel ?? string.Empty;
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(actionName) && !string.IsNullOrWhiteSpace(bindingPath);
}
