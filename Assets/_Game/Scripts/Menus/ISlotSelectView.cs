using System;
using System.Collections.Generic;

/// <summary>
/// The three save slots as the menu logic sees them: pick one to play, delete one, or go back.
/// The view only displays SlotSummary snapshots (SlotCardText has the wording) and reports choices.
/// </summary>
public interface ISlotSelectView : IMenuView
{
    event Action<int> SlotChosen;
    event Action<int> DeleteRequested;
    event Action BackRequested;

    /// <summary>Redraws the slots without showing or hiding anything (also used after a delete).</summary>
    void SetSlots(IReadOnlyList<SlotSummary> slots, int preselectedSlot);
}
