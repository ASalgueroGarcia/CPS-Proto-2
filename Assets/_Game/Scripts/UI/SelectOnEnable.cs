using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Selects a button whenever this panel opens, so a controller (or the keyboard arrows) can move
/// between its buttons straight away. Without a selection, the stick and D-pad have nothing to move from.
/// </summary>
public class SelectOnEnable : MonoBehaviour
{
    [SerializeField] private Selectable firstSelected;

    private void OnEnable()
    {
        if (firstSelected == null || EventSystem.current == null) return;

        // Clear first: re-selecting the object that is already selected is a no-op,
        // and the button would then not show its selected state on the second open.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
    }
}
