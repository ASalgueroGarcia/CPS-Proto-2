using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Turns the UI Cancel input (Esc, gamepad B) on a menu control into that menu's "back".
/// Cancel only reaches the selected control, so a dialog on top answers it and the screen behind
/// it doesn't react.
/// </summary>
public class CancelRelay : MonoBehaviour, ICancelHandler
{
    private Action _onCancel;

    /// <summary>Gives every control in <paramref name="panel"/> the same Cancel action.</summary>
    public static void AddTo(GameObject panel, Action onCancel)
    {
        foreach (Selectable selectable in panel.GetComponentsInChildren<Selectable>(true))
        {
            // The items of an open dropdown list close the list on Cancel by themselves.
            TMP_Dropdown dropdown = selectable.GetComponentInParent<TMP_Dropdown>(true);
            if (dropdown != null && dropdown.gameObject != selectable.gameObject) continue;

            selectable.gameObject.AddComponent<CancelRelay>()._onCancel = onCancel;
        }
    }

    public void OnCancel(BaseEventData eventData) => _onCancel?.Invoke();
}
