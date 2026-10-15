using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Canvas version of ISlotSelectView: three SlotCards and a Back button.
/// It only displays slots and reports choices; SlotSelectPresenter does the rest.
/// </summary>
public class SlotSelectCanvasView : MonoBehaviour, ISlotSelectView
{
    [SerializeField] private GameObject panel;
    [Tooltip("One card per save slot, in slot order.")]
    [SerializeField] private SlotCard[] cards;
    [SerializeField] private Button backButton;

    public event Action<int> SlotChosen;
    public event Action<int> DeleteRequested;
    public event Action BackRequested;

    private int _preselected;

    private void Awake()
    {
        for (int slot = 0; slot < cards.Length; slot++)
        {
            cards[slot].Bind(slot, chosen => SlotChosen?.Invoke(chosen), deleted => DeleteRequested?.Invoke(deleted));
        }

        CancelRelay.AddTo(panel, Back);
    }

    /// <summary>Back button OnClick target.</summary>
    public void Back() => BackRequested?.Invoke();

    public void SetSlots(IReadOnlyList<SlotSummary> slots, int preselectedSlot)
    {
        for (int slot = 0; slot < cards.Length && slot < slots.Count; slot++)
        {
            cards[slot].Show(slots[slot]);
        }

        _preselected = preselectedSlot;

        // Redrawn while up (after a delete): the focused DELETE may just have been hidden.
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (panel.activeInHierarchy && (selected == null || !selected.activeInHierarchy))
        {
            SelectPreselected();
        }
    }

    public void Show(Action onShown = null)
    {
        panel.SetActive(true);
        SelectPreselected();
        onShown?.Invoke();
    }

    public void Hide(Action onHidden = null)
    {
        panel.SetActive(false);
        onHidden?.Invoke();
    }

    private void SelectPreselected()
    {
        Selectable first = _preselected >= 0 && _preselected < cards.Length
            ? cards[_preselected].PlayButton
            : backButton;
        if (first != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
    }
}
