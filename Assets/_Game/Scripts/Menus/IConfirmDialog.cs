using System;

/// <summary>A yes / no question for destructive actions (deleting a save slot).</summary>
public interface IConfirmDialog
{
    void Ask(string message, Action<bool> onAnswer);
}
