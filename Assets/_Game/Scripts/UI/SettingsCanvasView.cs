using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Canvas version of ISettingsView (GDD > Menus > Settings Menu): Master / Music / SFX
/// sliders, resolution and window mode dropdowns, live Keybinds page, Back. Reports changes;
/// SettingsPresenter applies and saves them. The menu is a book: it opens when shown and closes
/// before it leaves.
/// </summary>
public class SettingsCanvasView : MonoBehaviour, ISettingsView
{
    [SerializeField] private GameObject panel;

    [Header("Audio")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Video")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown displayModeDropdown;

    [Header("Controls & Navigation")]
    [SerializeField] private KeybindsPanel keybindsPanel;
    [SerializeField] private Button backButton;

    [Header("Default Selection (keyboard / gamepad)")]
    [SerializeField] private Selectable firstSelected;

    [Header("Book (optional)")]
    [Tooltip("Scaled sideways to open and close the book. Leave empty for no animation.")]
    [SerializeField] private RectTransform book;
    [Tooltip("Seconds to open or close, on unscaled time so it also plays while paused.")]
    [Min(0f)]
    [SerializeField] private float bookSeconds = 0.25f;

    private Coroutine bookMove;

    public event Action<float> MasterVolumeChanged;
    public event Action<float> MusicVolumeChanged;
    public event Action<float> SfxVolumeChanged;
    public event Action<int> ResolutionChanged;
    public event Action<int> DisplayModeChanged;
    public event Action BackRequested;

    private void Awake()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(value => MasterVolumeChanged?.Invoke(value));
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(value => MusicVolumeChanged?.Invoke(value));
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(value => SfxVolumeChanged?.Invoke(value));
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(index => ResolutionChanged?.Invoke(index));
        if (displayModeDropdown != null)
            displayModeDropdown.onValueChanged.AddListener(index => DisplayModeChanged?.Invoke(index));

        if (keybindsPanel == null)
            keybindsPanel = GetComponent<KeybindsPanel>();

        if (keybindsPanel != null)
            keybindsPanel.SchemeVisibilityChanged += OnSchemeVisibilityChanged;

        if (panel != null)
            CancelRelay.AddTo(panel, HandleCancel);
    }

    private void OnDestroy()
    {
        if (keybindsPanel != null)
            keybindsPanel.SchemeVisibilityChanged -= OnSchemeVisibilityChanged;
    }

    /// <summary>Back button OnClick target.</summary>
    public void Back()
    {
        if (keybindsPanel != null && keybindsPanel.IsRebinding)
        {
            keybindsPanel.CancelRebindIfActive();
            return;
        }

        BackRequested?.Invoke();
    }

    public void SetState(SettingsViewState state)
    {
        // WithoutNotify: drawing the current values must not echo back as changes.
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(state.MasterVolume);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(state.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(state.SfxVolume);
        if (resolutionDropdown != null) Fill(resolutionDropdown, state.ResolutionOptions, state.ResolutionIndex);
        if (displayModeDropdown != null) Fill(displayModeDropdown, state.DisplayModeOptions, state.DisplayModeIndex);
    }

    public void Show(Action onShown = null)
    {
        panel.SetActive(true);
        if (keybindsPanel != null)
            keybindsPanel.RefreshAll();

        RebuildNavigation();
        MenuInputGuard.Hold(this);

        if (firstSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }

        MoveBook(0f, 1f, onShown);
    }

    public void Hide(Action onHidden = null)
    {
        if (keybindsPanel != null)
            keybindsPanel.CancelRebindIfActive();

        if (!panel.activeSelf)
        {
            onHidden?.Invoke();
            return;
        }

        MoveBook(1f, 0f, () =>
        {
            panel.SetActive(false);
            onHidden?.Invoke();
        });
    }

    /// <summary>Rebuilds explicit gamepad/keyboard navigation across Audio, Video, Controls, and Back.</summary>
    public void RebuildNavigation()
    {
        var verticalOrder = new List<Selectable>();
        AddIfInteractable(verticalOrder, masterSlider);
        AddIfInteractable(verticalOrder, musicSlider);
        AddIfInteractable(verticalOrder, sfxSlider);
        AddIfInteractable(verticalOrder, resolutionDropdown);
        AddIfInteractable(verticalOrder, displayModeDropdown);

        bool gamepadVisible = keybindsPanel != null && keybindsPanel.IsGamepadSchemeActive;
        Selectable presetPrev = gamepadVisible ? keybindsPanel.PresetPreviousButton : null;
        Selectable presetNext = gamepadVisible ? keybindsPanel.PresetNextButton : null;
        var rebindButtons = new List<Selectable>();

        if (gamepadVisible)
        {
            AddIfInteractable(verticalOrder, presetPrev);

            IReadOnlyList<RebindActionRow> rows = keybindsPanel.RebindRows;
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i] != null && rows[i].rebindButton != null && rows[i].rebindButton.IsInteractable())
                    {
                        rebindButtons.Add(rows[i].rebindButton);
                        verticalOrder.Add(rows[i].rebindButton);
                    }
                }
            }
        }

        AddIfInteractable(verticalOrder, backButton);

        if (verticalOrder.Count == 0)
            return;

        for (int i = 0; i < verticalOrder.Count; i++)
        {
            Selectable current = verticalOrder[i];
            Selectable up = verticalOrder[(i - 1 + verticalOrder.Count) % verticalOrder.Count];
            Selectable down = verticalOrder[(i + 1) % verticalOrder.Count];

            Navigation nav = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down
            };

            current.navigation = nav;
        }

        if (gamepadVisible)
        {
            Selectable firstRebind = rebindButtons.Count > 0 ? rebindButtons[0] : backButton;
            Selectable lastRebind = rebindButtons.Count > 0 ? rebindButtons[rebindButtons.Count - 1] : presetPrev;

            if (resolutionDropdown != null && presetPrev != null)
                SetHorizontal(resolutionDropdown, null, presetPrev);

            if (displayModeDropdown != null && firstRebind != null)
                SetHorizontal(displayModeDropdown, null, firstRebind);

            if (presetPrev != null)
            {
                SetHorizontal(presetPrev, resolutionDropdown, presetNext);
            }

            if (presetNext != null)
            {
                Navigation nextNav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = displayModeDropdown != null ? displayModeDropdown : sfxSlider,
                    selectOnDown = firstRebind,
                    selectOnLeft = presetPrev,
                    selectOnRight = null
                };
                presetNext.navigation = nextNav;
            }

            for (int i = 0; i < rebindButtons.Count; i++)
            {
                SetHorizontal(rebindButtons[i], displayModeDropdown, null);
            }

            if (backButton != null && lastRebind != null)
                SetHorizontal(backButton, null, lastRebind);
        }
    }

    private void OnSchemeVisibilityChanged(bool gamepadVisible)
    {
        RebuildNavigation();

        if (!gamepadVisible && EventSystem.current != null)
        {
            GameObject current = EventSystem.current.currentSelectedGameObject;
            if (current != null && !current.activeInHierarchy && firstSelected != null && firstSelected.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
            }
        }
    }

    private void HandleCancel()
    {
        // While listening for a gamepad rebind (or on the frame it just ended), do not cancel
        // the rebind or close Settings: gamepad B must be bindable and Esc cancels via the rebind op.
        if (keybindsPanel != null && keybindsPanel.SuppressCancelInput)
            return;

        Back();
    }

    private void MoveBook(float from, float to, Action onDone)
    {
        if (bookMove != null)
            StopCoroutine(bookMove);

        if (book == null || bookSeconds <= 0f || !isActiveAndEnabled || !Application.isPlaying)
        {
            if (book != null)
                book.localScale = Vector3.one;
            onDone?.Invoke();
            return;
        }

        bookMove = StartCoroutine(BookRoutine(from, to, onDone));
    }

    private IEnumerator BookRoutine(float from, float to, Action onDone)
    {
        for (float t = 0f; t < bookSeconds; t += Time.unscaledDeltaTime)
        {
            book.localScale = new Vector3(Mathf.SmoothStep(from, to, t / bookSeconds), 1f, 1f);
            yield return null;
        }

        // Left at full size so the next open starts from a clean pose.
        book.localScale = Vector3.one;
        bookMove = null;
        onDone?.Invoke();
    }

    private static void AddIfInteractable(List<Selectable> list, Selectable selectable)
    {
        if (selectable != null && selectable.IsInteractable())
            list.Add(selectable);
    }

    private static void SetHorizontal(Selectable target, Selectable left, Selectable right)
    {
        if (target == null)
            return;

        Navigation nav = target.navigation;
        nav.selectOnLeft = left;
        nav.selectOnRight = right;
        target.navigation = nav;
    }

    private static void Fill(TMP_Dropdown dropdown, IReadOnlyList<string> options, int index)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(options));
        dropdown.SetValueWithoutNotify(index);
        dropdown.RefreshShownValue();
    }
}
