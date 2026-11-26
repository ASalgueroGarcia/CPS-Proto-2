using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Keybinds page of the settings book (GDD > Menus > Settings Menu). Auto-detects Keyboard/Mouse
/// vs Gamepad via InputDeviceTracker, switches between stylized non-1:1 diagrams, drives modular
/// controller layout presets from ControllerPresetCatalog, and runs interactive per-action rebinds.
/// </summary>
public class KeybindsPanel : MonoBehaviour
{
    private const string PauseActionName = "Pause";

    private static ControllerRebindService sharedRebindService;
    private static InputActionAsset sharedActionAsset;

    [Header("Schemes")]
    [SerializeField] private GameObject keyboardScheme;
    [SerializeField] private GameObject gamepadScheme;
    [SerializeField] private TMP_Text activeDeviceLabel;

    [Header("Visualizers")]
    [SerializeField] private KeyboardMouseDiagramView keyboardDiagram;
    [SerializeField] private ControllerVisualizerView controllerVisualizer;

    [Header("Controller Presets & Rebinding")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private ControllerPresetCatalog presetCatalog;
    [SerializeField] private Button presetPreviousButton;
    [SerializeField] private Button presetNextButton;
    [SerializeField] private TMP_Text presetNameLabel;
    [SerializeField] private TMP_Text rebindStatusLabel;
    [SerializeField] private List<RebindActionRow> rebindRows = new List<RebindActionRow>();

    private ControllerRebindService rebindService;
    private InputDeviceTracker deviceTracker;
    private bool ownsRebindService;
    private bool listenersWired;
    private bool trackerSubscribed;
    private bool serviceSubscribed;
    private string listeningActionName;
    private int lastRebindFinishFrame = -10;

    public bool IsGamepadSchemeActive { get; private set; }
    public bool IsRebinding => rebindService != null && rebindService.IsRebinding;
    public bool SuppressCancelInput => IsRebinding || (Time.frameCount - lastRebindFinishFrame) <= 1;
    public string ActivePresetDisplayName => presetNameLabel != null ? presetNameLabel.text : ResolvePresetDisplayName();
    public ControllerRebindService RebindService => rebindService;
    public ControllerPresetCatalog PresetCatalog => presetCatalog;
    public Button PresetPreviousButton => presetPreviousButton;
    public Button PresetNextButton => presetNextButton;
    public IReadOnlyList<RebindActionRow> RebindRows => rebindRows;

    public event Action<bool> SchemeVisibilityChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSharedService()
    {
        sharedRebindService?.Dispose();
        sharedRebindService = null;
        sharedActionAsset = null;
    }

    public static void ResetSharedServiceForTests() => ResetSharedService();

    private void Awake()
    {
        WireButtonListeners();
    }

    private void Start()
    {
        EnsureInitialized();
        RefreshAll();
    }

    private void OnEnable()
    {
        WireButtonListeners();
        EnsureInitialized();
        SubscribeTracker();
        SubscribeRebindService();
        RefreshAll();
    }

    private void OnDisable()
    {
        CancelRebindIfActive();
        UnsubscribeTracker();
        UnsubscribeRebindService();
    }

    private void OnDestroy()
    {
        CancelRebindIfActive();
        UnsubscribeTracker();
        UnsubscribeRebindService();

        if (ownsRebindService && rebindService != null && rebindService != sharedRebindService)
        {
            rebindService.Dispose();
        }
    }

    /// <summary>Injects explicit service/tracker instances (used by EditMode tests or custom hosts).</summary>
    public void Initialize(
        ControllerRebindService customService,
        InputDeviceTracker customTracker = null,
        ControllerPresetCatalog customCatalog = null)
    {
        UnsubscribeTracker();
        UnsubscribeRebindService();

        if (ownsRebindService && rebindService != null && rebindService != sharedRebindService)
            rebindService.Dispose();

        rebindService = customService;
        ownsRebindService = false;

        if (customCatalog != null)
            presetCatalog = customCatalog;
        else if (customService != null && customService.Catalog != null)
            presetCatalog = customService.Catalog;

        if (customTracker != null)
            deviceTracker = customTracker;

        WireButtonListeners();
        SubscribeTracker();
        SubscribeRebindService();
        RefreshAll();
    }

    /// <summary>Steps forward to the next preset in ControllerPresetCatalog (wrapping around).</summary>
    public void SelectNextPreset() => CyclePreset(1);

    /// <summary>Steps backward to the previous preset in ControllerPresetCatalog (wrapping around).</summary>
    public void SelectPreviousPreset() => CyclePreset(-1);

    /// <summary>Cycles modularly through all valid presets configured in ControllerPresetCatalog.</summary>
    public void CyclePreset(int direction)
    {
        EnsureInitialized();
        if (rebindService == null || direction == 0)
            return;

        List<ControllerPreset> presets = CollectAvailablePresets();
        if (presets.Count == 0)
            return;

        CancelRebindIfActive();

        int step = direction > 0 ? 1 : -1;
        int nextIndex;

        if (ControllerPresetCatalog.IsCustomPreset(rebindService.ActivePresetId))
        {
            nextIndex = step > 0 ? 0 : presets.Count - 1;
        }
        else
        {
            int currentIndex = FindPresetIndex(presets, rebindService.ActivePresetId);
            if (currentIndex < 0)
                currentIndex = 0;

            nextIndex = (currentIndex + step + presets.Count) % presets.Count;
        }

        rebindService.ApplyPreset(presets[nextIndex].id);
        RefreshAll();
    }

    /// <summary>Starts listening for a single gamepad control to rebind the requested action.</summary>
    public bool BeginRebind(string actionName)
    {
        EnsureInitialized();
        if (rebindService == null || string.IsNullOrWhiteSpace(actionName))
            return false;

        string resolvedAction = ResolveActionName(actionName);
        if (string.IsNullOrWhiteSpace(resolvedAction))
            return false;

        bool started = rebindService.StartInteractiveRebind(
            resolvedAction,
            onCompleted: _ =>
            {
                listeningActionName = null;
                lastRebindFinishFrame = Time.frameCount;
                RefreshAll();
            },
            onCanceled: () =>
            {
                listeningActionName = null;
                lastRebindFinishFrame = Time.frameCount;
                RefreshAll();
            },
            cancelBindingPath: "<Keyboard>/escape");

        if (!started)
            return false;

        listeningActionName = actionName;
        RefreshBindingsUI();
        return true;
    }

    /// <summary>Cancels any active interactive rebind cleanly without closing the settings menu.</summary>
    public void CancelRebindIfActive()
    {
        if (rebindService == null || !rebindService.IsRebinding)
            return;

        listeningActionName = null;
        lastRebindFinishFrame = Time.frameCount;
        rebindService.CancelInteractiveRebind();
        RefreshBindingsUI();
    }

    /// <summary>Syncs active input scheme visibility, preset label, rebind rows, and live diagrams.</summary>
    public void RefreshAll()
    {
        EnsureInitialized();

        InputDeviceKind activeDevice = deviceTracker != null
            ? deviceTracker.CurrentDevice
            : InputDeviceKind.KeyboardMouse;
        bool useGamepad = activeDevice == InputDeviceKind.Gamepad;

        if (!useGamepad && IsRebinding)
            CancelRebindIfActive();

        bool schemeChanged = IsGamepadSchemeActive != useGamepad;
        IsGamepadSchemeActive = useGamepad;

        if (keyboardScheme != null)
            keyboardScheme.SetActive(!useGamepad);

        if (gamepadScheme != null)
            gamepadScheme.SetActive(useGamepad);

        UpdateActiveDeviceHeader(activeDevice, ResolveGamepadFamily());
        RefreshBindingsUI();

        if (schemeChanged)
            SchemeVisibilityChanged?.Invoke(IsGamepadSchemeActive);
    }

    private void EnsureInitialized()
    {
        if (deviceTracker == null)
        {
            deviceTracker = InputDeviceTracker.Instance != null
                ? InputDeviceTracker.Instance
                : InputDeviceTracker.EnsureInstance();
        }

        if (rebindService == null)
        {
            InputActionAsset actions = inputActions != null ? inputActions : InputSystem.actions;
            if (actions != null)
            {
                if (sharedRebindService != null && sharedActionAsset == actions)
                {
                    rebindService = sharedRebindService;
                    ownsRebindService = false;
                }
                else
                {
                    rebindService = new ControllerRebindService(actions, presetCatalog, Services.Settings);
                    rebindService.LoadSavedBindings();
                    sharedRebindService = rebindService;
                    sharedActionAsset = actions;
                    ownsRebindService = false;
                }
            }
        }
    }

    private void WireButtonListeners()
    {
        if (listenersWired)
            return;

        listenersWired = true;

        if (presetPreviousButton != null)
            presetPreviousButton.onClick.AddListener(SelectPreviousPreset);

        if (presetNextButton != null)
            presetNextButton.onClick.AddListener(SelectNextPreset);

        for (int i = 0; i < rebindRows.Count; i++)
        {
            RebindActionRow row = rebindRows[i];
            if (row == null || row.rebindButton == null || string.IsNullOrWhiteSpace(row.actionName))
                continue;

            string capturedAction = row.actionName;
            row.rebindButton.onClick.AddListener(() => BeginRebind(capturedAction));
        }
    }

    private void SubscribeTracker()
    {
        if (trackerSubscribed || deviceTracker == null)
            return;

        trackerSubscribed = true;
        deviceTracker.DeviceChanged += OnDeviceChanged;
        deviceTracker.GamepadFamilyChanged += OnGamepadFamilyChanged;
    }

    private void UnsubscribeTracker()
    {
        if (!trackerSubscribed || deviceTracker == null)
            return;

        trackerSubscribed = false;
        deviceTracker.DeviceChanged -= OnDeviceChanged;
        deviceTracker.GamepadFamilyChanged -= OnGamepadFamilyChanged;
    }

    private void SubscribeRebindService()
    {
        if (serviceSubscribed || rebindService == null)
            return;

        serviceSubscribed = true;
        rebindService.BindingsChanged += OnBindingsChanged;
    }

    private void UnsubscribeRebindService()
    {
        if (!serviceSubscribed || rebindService == null)
            return;

        serviceSubscribed = false;
        rebindService.BindingsChanged -= OnBindingsChanged;
    }

    private void OnDeviceChanged(InputDeviceKind device)
    {
        RefreshAll();
    }

    private void OnGamepadFamilyChanged(GamepadFamily family)
    {
        RefreshAll();
    }

    private void OnBindingsChanged(string activePresetId)
    {
        RefreshBindingsUI();
    }

    private void RefreshBindingsUI()
    {
        GamepadFamily family = ResolveGamepadFamily();

        if (presetNameLabel != null)
            presetNameLabel.text = ResolvePresetDisplayName();

        if (rebindStatusLabel != null)
        {
            rebindStatusLabel.text = !string.IsNullOrEmpty(listeningActionName)
                ? $"PRESS GAMEPAD BUTTON FOR {listeningActionName.ToUpperInvariant()} (ESC CANCELS)"
                : "SELECT AN ACTION TO REBIND  |  LIVE INPUT TESTER";
        }

        for (int i = 0; i < rebindRows.Count; i++)
        {
            RebindActionRow row = rebindRows[i];
            if (row == null || row.bindingLabel == null)
                continue;

            bool isListeningRow = !string.IsNullOrEmpty(listeningActionName)
                                  && string.Equals(listeningActionName, row.actionName, StringComparison.OrdinalIgnoreCase);

            if (isListeningRow)
            {
                row.bindingLabel.text = "PRESS...";
                continue;
            }

            string path = ResolveEffectivePath(row.actionName);
            string formatted = InputDeviceTracker.FormatGamepadBindingLabel(path, family);
            row.bindingLabel.text = string.IsNullOrWhiteSpace(formatted) ? "-" : formatted;
        }

        if (keyboardDiagram != null)
            keyboardDiagram.RefreshBindings(rebindService);

        if (controllerVisualizer != null)
            controllerVisualizer.RefreshBindings(rebindService, family, listeningActionName);
    }

    private void UpdateActiveDeviceHeader(InputDeviceKind activeDevice, GamepadFamily family)
    {
        if (activeDeviceLabel == null)
            return;

        if (activeDevice == InputDeviceKind.KeyboardMouse)
        {
            activeDeviceLabel.text = "INPUT: KEYBOARD & MOUSE";
            return;
        }

        string familyText = family switch
        {
            GamepadFamily.PlayStation => "PLAYSTATION GAMEPAD",
            GamepadFamily.Xbox => "XBOX GAMEPAD",
            _ => "GAMEPAD"
        };
        activeDeviceLabel.text = $"INPUT: {familyText}";
    }

    private string ResolvePresetDisplayName()
    {
        string activeId = rebindService != null
            ? rebindService.ActivePresetId
            : ControllerPresetCatalog.DefaultPresetId;

        if (ControllerPresetCatalog.IsCustomPreset(activeId))
            return "Custom";

        ControllerPresetCatalog catalog = presetCatalog != null
            ? presetCatalog
            : rebindService?.Catalog;

        if (catalog != null && catalog.TryGetPreset(activeId, out ControllerPreset match))
            return !string.IsNullOrWhiteSpace(match.displayName) ? match.displayName : match.id;

        if (string.Equals(activeId, ControllerPresetCatalog.ActionPresetId, StringComparison.OrdinalIgnoreCase))
            return "Action (Shoulders)";

        return "Classic (GDD)";
    }

    private List<ControllerPreset> CollectAvailablePresets()
    {
        var result = new List<ControllerPreset>();
        ControllerPresetCatalog catalog = presetCatalog != null
            ? presetCatalog
            : rebindService?.Catalog;

        if (catalog != null && catalog.Presets != null)
        {
            for (int i = 0; i < catalog.Presets.Count; i++)
            {
                ControllerPreset candidate = catalog.Presets[i];
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.id))
                    continue;

                if (ControllerPresetCatalog.IsCustomPreset(candidate.id))
                    continue;

                result.Add(candidate);
            }
        }

        if (result.Count == 0)
        {
            result.Add(ControllerPreset.CreateClassic());
            result.Add(ControllerPreset.CreateAction());
        }

        return result;
    }

    private static int FindPresetIndex(List<ControllerPreset> presets, string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
            return -1;

        for (int i = 0; i < presets.Count; i++)
        {
            if (string.Equals(presets[i].id, presetId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private string ResolveActionName(string requested)
    {
        if (rebindService == null || rebindService.ActionAsset == null)
            return requested;

        if (ControllerRebindService.FindAction(rebindService.ActionAsset, requested) != null)
            return requested;

        if (requested.Equals(PauseActionName, StringComparison.OrdinalIgnoreCase)
            && ControllerRebindService.FindAction(rebindService.ActionAsset, "PAUSE") != null)
        {
            return "PAUSE";
        }

        return requested;
    }

    private string ResolveEffectivePath(string actionName)
    {
        if (rebindService == null)
            return string.Empty;

        string path = rebindService.GetEffectiveGamepadPath(actionName);
        if (string.IsNullOrWhiteSpace(path) && actionName.Equals(PauseActionName, StringComparison.OrdinalIgnoreCase))
            path = rebindService.GetEffectiveGamepadPath("PAUSE");

        return path;
    }

    private GamepadFamily ResolveGamepadFamily()
    {
        if (deviceTracker != null)
            return deviceTracker.CurrentGamepadFamily;

        return Gamepad.current != null
            ? InputDeviceTracker.DetectGamepadFamily(Gamepad.current)
            : GamepadFamily.Xbox;
    }
}
