using System;
using UnityEngine;

/// <summary>
/// Save slot screen logic: shows the three slots, plays the chosen one (an empty slot starts
/// a new game at starting values) and deletes one after a confirmation.
/// </summary>
public class SlotSelectPresenter : IDisposable
{
    private readonly ISlotSelectView _view;
    private readonly IConfirmDialog _confirm;
    private readonly SaveService _save;
    private readonly IGameFlow _flow;
    private Action _onBack;

    public SlotSelectPresenter(ISlotSelectView view, IConfirmDialog confirm, SaveService save, IGameFlow flow)
    {
        _view = view;
        _confirm = confirm;
        _save = save;
        _flow = flow;

        _view.SlotChosen += OnSlotChosen;
        _view.DeleteRequested += OnDeleteRequested;
        _view.BackRequested += OnBack;
    }

    public void Dispose()
    {
        _view.SlotChosen -= OnSlotChosen;
        _view.DeleteRequested -= OnDeleteRequested;
        _view.BackRequested -= OnBack;
    }

    /// <summary>Shows the slots; <paramref name="onBack"/> runs when the player backs out.</summary>
    public void Open(Action onBack)
    {
        _onBack = onBack;
        Refresh();
    }

    private void Refresh()
    {
        var slots = new SlotSummary[SaveService.SlotCount];
        for (int slot = 0; slot < slots.Length; slot++)
        {
            slots[slot] = _save.Peek(slot);
        }

        _view.Show(slots, _save.HasActiveSlot ? _save.ActiveSlot : 0);
    }

    private void OnSlotChosen(int slot)
    {
        if (!SaveService.IsValidSlot(slot)) return;
        if (_flow == null)
        {
            Debug.LogError("[SlotSelectPresenter] No IGameFlow registered - can't enter the game.");
            return;
        }

        _save.SelectSlot(slot);
        _save.MarkFirstLaunchDone();
        _view.Hide();
        _flow.EnterHub();
    }

    private void OnDeleteRequested(int slot)
    {
        if (!_save.Peek(slot).Exists) return;

        _confirm.Ask($"Delete save {slot + 1}? This can't be undone.", confirmed =>
        {
            if (!confirmed) return;

            _save.DeleteSlot(slot);
            Refresh();
        });
    }

    private void OnBack()
    {
        _view.Hide();
        _onBack?.Invoke();
    }
}
