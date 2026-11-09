using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the title screen's menu logic from whichever views are dropped into its slots.
/// To move a menu into the world (diegetic UI), drag a different component implementing the
/// same interface into its slot - presenters and services stay untouched.
/// </summary>
public class MainMenuInstaller : MonoBehaviour
{
    [Header("Views (any component implementing the interface)")]
    [Tooltip("IMainMenuView - the title screen.")]
    [SerializeField] private MonoBehaviour mainMenuView;
    [Tooltip("ISlotSelectView - the three save slots.")]
    [SerializeField] private MonoBehaviour slotSelectView;
    [Tooltip("ISettingsView - the settings menu.")]
    [SerializeField] private MonoBehaviour settingsView;
    [Tooltip("IConfirmDialog - yes / no for deleting a slot.")]
    [SerializeField] private MonoBehaviour confirmDialog;

    private readonly List<System.IDisposable> _presenters = new List<System.IDisposable>();

    private void Start()
    {
        if (Services.Save == null || Services.Settings == null)
        {
            Debug.LogError("[MainMenuInstaller] Save services are missing - the menu can't start.", this);
            return;
        }

        var menu = Require<IMainMenuView>(mainMenuView, nameof(mainMenuView));
        var slots = Require<ISlotSelectView>(slotSelectView, nameof(slotSelectView));
        var settings = Require<ISettingsView>(settingsView, nameof(settingsView));
        var confirm = Require<IConfirmDialog>(confirmDialog, nameof(confirmDialog));
        if (menu == null || slots == null || settings == null || confirm == null) return;

        var slotPresenter = new SlotSelectPresenter(slots, confirm, Services.Save, Services.Flow);
        var settingsPresenter = new SettingsPresenter(settings, Services.Settings);
        var mainPresenter = new MainMenuPresenter(menu, slotPresenter, settingsPresenter, Services.Flow);
        _presenters.Add(slotPresenter);
        _presenters.Add(settingsPresenter);
        _presenters.Add(mainPresenter);

        slots.Hide();
        settings.Hide();
        mainPresenter.Open();
    }

    private void OnDestroy()
    {
        foreach (System.IDisposable presenter in _presenters)
        {
            presenter.Dispose();
        }

        _presenters.Clear();
    }

    private void OnValidate()
    {
        mainMenuView = FindImplementation<IMainMenuView>(mainMenuView);
        slotSelectView = FindImplementation<ISlotSelectView>(slotSelectView);
        settingsView = FindImplementation<ISettingsView>(settingsView);
        confirmDialog = FindImplementation<IConfirmDialog>(confirmDialog);
    }

    private T Require<T>(MonoBehaviour component, string field) where T : class
    {
        if (component is T view) return view;

        Debug.LogError($"[MainMenuInstaller] '{field}' needs a component implementing {typeof(T).Name}.", this);
        return null;
    }

    // Dragging a GameObject into a MonoBehaviour field picks its first MonoBehaviour, which is
    // often not the view - swap in the component on that object that implements the interface.
    private static MonoBehaviour FindImplementation<T>(MonoBehaviour candidate) where T : class
    {
        if (candidate == null || candidate is T) return candidate;

        foreach (MonoBehaviour component in candidate.GetComponents<MonoBehaviour>())
        {
            if (component is T) return component;
        }

        Debug.LogWarning($"[MainMenuInstaller] {candidate.name} has no component implementing {typeof(T).Name}.", candidate);
        return null;
    }
}
