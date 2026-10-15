using System;
using UnityEngine;

/// <summary>
/// Save slot screen logic: shows the three slots, plays the chosen one (an empty slot starts
/// a new game at starting values) and deletes one after a confirmation. Input is ignored while
/// the view animates or the delete question is open, so a double press can't start two games.
/// </summary>
public class SlotSelectPresenter : IDisposable
{
    private readonly ISlotSelectView _view;
    private readonly IConfirmDialog _confirm;
    private readonly SaveService _save;
    private readonly IGameFlow _flow;
    private Action _onBack;
    private bool _busy = true;

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

    /// <summary>Shows the slots; <paramref name="onBack"/> runs once the player has backed out.</summary>
    public void Open(Action onBack)
    {
        _onBack = onBack;
        _busy = true;
        Refresh();
        _view.Show(() => _busy = false);
    }

    private void Refresh()
    {
        var slots = new SlotSummary[SaveService.SlotCount];
        for (int slot = 0; slot < slots.Length; slot++)
        {
            slots[slot] = _save.Peek(slot);
        }

        _view.SetSlots(slots, _save.HasActiveSlot ? _save.ActiveSlot : 0);
    }

    private void OnSlotChosen(int slot)
    {
        if (_busy || !SaveService.IsValidSlot(slot)) return;
        if (_flow == null)
        {
            Debug.LogError("[SlotSelectPresenter] No IGameFlow registered - can't enter the game.");
            return;
        }

        // A file another program holds can't be read: playing it would start from nothing and save
        // nothing. Read it again instead - if it opens now, the card updates and the next press plays.
        if (_save.Peek(slot).Locked)
        {
            Refresh();
            return;
        }

        _busy = true;
        _save.SelectSlot(slot);
        _view.Hide(_flow.EnterHub);
    }

    private void OnDeleteRequested(int slot)
    {
        if (_busy || !_save.Peek(slot).Exists) return;

        _busy = true;
        _confirm.Ask($"Delete save {slot + 1}? This can't be undone.", confirmed =>
        {
            if (confirmed)
            {
                _save.DeleteSlot(slot);
                Refresh();
            }

            _busy = false;
        });
    }

    private void OnBack()
    {
        if (_busy) return;
        _busy = true;
        _view.Hide(() => _onBack?.Invoke());
    }
}
