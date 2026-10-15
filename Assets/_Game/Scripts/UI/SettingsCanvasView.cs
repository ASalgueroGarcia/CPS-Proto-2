using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Canvas version of ISettingsView (GDD > Menus > Settings Menu): Master / Music / SFX
/// sliders, resolution and window mode dropdowns, Back. Reports changes; SettingsPresenter
/// applies and saves them.
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

    [Header("Default Selection (keyboard / gamepad)")]
    [SerializeField] private Selectable firstSelected;

    public event Action<float> MasterVolumeChanged;
    public event Action<float> MusicVolumeChanged;
    public event Action<float> SfxVolumeChanged;
    public event Action<int> ResolutionChanged;
    public event Action<int> DisplayModeChanged;
    public event Action BackRequested;

    private void Awake()
    {
        masterSlider.onValueChanged.AddListener(value => MasterVolumeChanged?.Invoke(value));
        musicSlider.onValueChanged.AddListener(value => MusicVolumeChanged?.Invoke(value));
        sfxSlider.onValueChanged.AddListener(value => SfxVolumeChanged?.Invoke(value));
        resolutionDropdown.onValueChanged.AddListener(index => ResolutionChanged?.Invoke(index));
        displayModeDropdown.onValueChanged.AddListener(index => DisplayModeChanged?.Invoke(index));
    }

    /// <summary>Back button OnClick target.</summary>
    public void Back() => BackRequested?.Invoke();

    public void SetState(SettingsViewState state)
    {
        // WithoutNotify: drawing the current values must not echo back as changes.
        masterSlider.SetValueWithoutNotify(state.MasterVolume);
        musicSlider.SetValueWithoutNotify(state.MusicVolume);
        sfxSlider.SetValueWithoutNotify(state.SfxVolume);
        Fill(resolutionDropdown, state.ResolutionOptions, state.ResolutionIndex);
        Fill(displayModeDropdown, state.DisplayModeOptions, state.DisplayModeIndex);
    }

    public void Show(Action onShown = null)
    {
        panel.SetActive(true);
        if (firstSelected != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }

        onShown?.Invoke();
    }

    public void Hide(Action onHidden = null)
    {
        panel.SetActive(false);
        onHidden?.Invoke();
    }

    private static void Fill(TMP_Dropdown dropdown, IReadOnlyList<string> options, int index)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(options));
        dropdown.SetValueWithoutNotify(index);
        dropdown.RefreshShownValue();
    }
}
