using System;
using System.Collections.Generic;

/// <summary>
/// The three save slots as the menu logic sees them: pick one to play, delete one, or go back.
/// The view only displays SlotSummary snapshots and reports what the player chose.
/// </summary>
public interface ISlotSelectView
{
    event Action<int> SlotChosen;
    event Action<int> DeleteRequested;
    event Action BackRequested;

    void Show(IReadOnlyList<SlotSummary> slots, int preselectedSlot);
    void Hide();
}
