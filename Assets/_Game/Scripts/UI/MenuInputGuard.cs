using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Two input rules the Canvas menu screens share, kept on the menu's root canvas:
/// - Right after a screen changes, pointer clicks are ignored for a moment. Otherwise the second
///   click of a double-click lands on whatever the new screen put under the cursor: a save's PLAY
///   under RESUME, the dialog's DELETE under the slot's DELETE, a slot under KEEP.
/// - Focus never stays empty. A click on the background clears the selection, and keyboard /
///   gamepad input (arrows, Submit, Esc / B) only reaches a selected control, so the last one is
///   selected again.
/// The views call Hold() whenever they show something; the guard adds itself the first time.
/// </summary>
public class MenuInputGuard : MonoBehaviour
{
    // Longer than a quick double-click, shorter than a deliberate second click.
    private const float ClickPauseSeconds = 0.35f;

    private CanvasGroup _group;
    private float _clicksBackAt = -1f;
    private GameObject _lastSelected;

    /// <summary>Ignores pointer clicks on the menu that <paramref name="view"/> belongs to for a moment.</summary>
    public static void Hold(Component view)
    {
        Canvas canvas = view.GetComponentInParent<Canvas>(true);
        if (canvas == null) return;

        GameObject root = canvas.rootCanvas.gameObject;
        if (!root.TryGetComponent(out MenuInputGuard guard))
        {
            guard = root.AddComponent<MenuInputGuard>();
        }

        guard.PauseClicks();
    }

    private void PauseClicks()
    {
        if (_group == null && !TryGetComponent(out _group))
        {
            _group = gameObject.AddComponent<CanvasGroup>();
        }

        _group.blocksRaycasts = false;
        _clicksBackAt = Time.unscaledTime + ClickPauseSeconds;
    }

    private void Update()
    {
        if (_clicksBackAt >= 0f && Time.unscaledTime >= _clicksBackAt)
        {
            _group.blocksRaycasts = true;
            _clicksBackAt = -1f;
        }

        KeepSelection();
    }

    private void KeepSelection()
    {
        EventSystem events = EventSystem.current;
        if (events == null) return;

        GameObject selected = events.currentSelectedGameObject;
        if (selected != null)
        {
            if (selected.activeInHierarchy && selected.transform.IsChildOf(transform)) _lastSelected = selected;
            return;
        }

        if (_lastSelected != null && _lastSelected.activeInHierarchy)
        {
            events.SetSelectedGameObject(_lastSelected);
        }
    }
}
