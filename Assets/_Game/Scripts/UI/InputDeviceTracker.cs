using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XInput;

/// <summary>
/// Always-on runtime input device detector. Switches automatically between KeyboardMouse and
/// Gamepad on real input activity (ignoring stick drift, gyro packets and tiny mouse jitter),
/// identifies PlayStation vs Xbox controller families, and raises DeviceChanged cleanly.
/// </summary>
public class InputDeviceTracker : MonoBehaviour
{
    [Header("Detection Thresholds")]
    [Range(0.05f, 0.8f)]
    [SerializeField] private float stickDeadzone = 0.2f;
    [Range(0.05f, 0.8f)]
    [SerializeField] private float triggerThreshold = 0.2f;
    [Min(0.5f)]
    [SerializeField] private float mouseMoveThreshold = 4f;

    private IDisposable eventSubscription;
    private IDisposable buttonPressSubscription;
    private bool pendingDevicePoll;
    private bool subscribed;

    public static InputDeviceTracker Instance { get; private set; }

    public InputDeviceKind CurrentDevice { get; private set; } = InputDeviceKind.KeyboardMouse;
    public GamepadFamily CurrentGamepadFamily { get; private set; } = GamepadFamily.Generic;
    public bool IsUsingGamepad => CurrentDevice == InputDeviceKind.Gamepad;

    public event Action<InputDeviceKind> DeviceChanged;
    public event Action<GamepadFamily> GamepadFamilyChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static InputDeviceTracker EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        var host = new GameObject("@InputDeviceTracker");
        if (Application.isPlaying)
            DontDestroyOnLoad(host);

        return host.AddComponent<InputDeviceTracker>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (Application.isPlaying)
            DontDestroyOnLoad(gameObject);

        if (Gamepad.current != null)
            CurrentGamepadFamily = DetectGamepadFamily(Gamepad.current);
    }

    private void OnEnable()
    {
        SubscribeInputCallbacks();
    }

    private void OnDisable()
    {
        UnsubscribeInputCallbacks();
    }

    private void OnDestroy()
    {
        UnsubscribeInputCallbacks();
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!pendingDevicePoll)
        {
            PollContinuousControls();
            return;
        }

        pendingDevicePoll = false;
        PollContinuousControls();
    }

    private void SubscribeInputCallbacks()
    {
        if (subscribed)
            return;

        subscribed = true;
        eventSubscription = InputSystem.onEvent.Call(OnInputEvent);
        buttonPressSubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        InputSystem.onActionChange += OnActionChange;
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void UnsubscribeInputCallbacks()
    {
        if (!subscribed)
            return;

        subscribed = false;
        eventSubscription?.Dispose();
        eventSubscription = null;
        buttonPressSubscription?.Dispose();
        buttonPressSubscription = null;
        InputSystem.onActionChange -= OnActionChange;
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnInputEvent(InputEventPtr eventPtr)
    {
        if (!eventPtr.valid)
            return;

        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        InputDevice device = InputSystem.GetDeviceById(eventPtr.deviceId);
        if (device is Gamepad || device is Keyboard || device is Mouse)
            pendingDevicePoll = true;
    }

    private void OnAnyButtonPressed(InputControl control)
    {
        if (control == null || control.device == null)
            return;

        NotifyDeviceActivity(control.device);
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed)
            return;

        if (!(obj is InputAction action) || action.activeControl == null)
            return;

        InputControl control = action.activeControl;
        InputDevice device = control.device;
        if (device is Gamepad)
        {
            if (control is StickControl stick && stick.ReadValue().sqrMagnitude < stickDeadzone * stickDeadzone)
                return;

            NotifyDeviceActivity(device);
        }
        else if (device is Keyboard || device is Mouse)
        {
            if (control is DeltaControl delta && delta.ReadValue().sqrMagnitude < mouseMoveThreshold * mouseMoveThreshold)
                return;

            NotifyDeviceActivity(device);
        }
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            {
                GamepadFamily detected = DetectGamepadFamily(device);
                if (CurrentDevice == InputDeviceKind.Gamepad)
                    SetActiveDevice(InputDeviceKind.Gamepad, detected);
                else
                    CurrentGamepadFamily = detected;
            }
            else if ((change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
                     && Gamepad.all.Count == 0
                     && CurrentDevice == InputDeviceKind.Gamepad)
            {
                SetActiveDevice(InputDeviceKind.KeyboardMouse, CurrentGamepadFamily);
            }
        }
    }

    private void PollContinuousControls()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && HasGamepadActuation(gamepad))
        {
            NotifyDeviceActivity(gamepad);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            NotifyDeviceActivity(keyboard);
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && HasMouseActuation(mouse))
        {
            NotifyDeviceActivity(mouse);
        }
    }

    private bool HasGamepadActuation(Gamepad gamepad)
    {
        float deadzoneSq = stickDeadzone * stickDeadzone;
        if (gamepad.leftStick.ReadValue().sqrMagnitude >= deadzoneSq)
            return true;

        if (gamepad.rightStick.ReadValue().sqrMagnitude >= deadzoneSq)
            return true;

        if (gamepad.dpad.ReadValue().sqrMagnitude >= 0.25f)
            return true;

        if (gamepad.leftTrigger.ReadValue() >= triggerThreshold || gamepad.rightTrigger.ReadValue() >= triggerThreshold)
            return true;

        return gamepad.buttonSouth.wasPressedThisFrame
               || gamepad.buttonNorth.wasPressedThisFrame
               || gamepad.buttonEast.wasPressedThisFrame
               || gamepad.buttonWest.wasPressedThisFrame
               || gamepad.leftShoulder.wasPressedThisFrame
               || gamepad.rightShoulder.wasPressedThisFrame
               || gamepad.startButton.wasPressedThisFrame
               || gamepad.selectButton.wasPressedThisFrame;
    }

    private bool HasMouseActuation(Mouse mouse)
    {
        if (mouse.leftButton.wasPressedThisFrame
            || mouse.rightButton.wasPressedThisFrame
            || mouse.middleButton.wasPressedThisFrame)
        {
            return true;
        }

        if (mouse.scroll.ReadValue().sqrMagnitude > 0.01f)
            return true;

        return mouse.delta.ReadValue().sqrMagnitude >= mouseMoveThreshold * mouseMoveThreshold;
    }

    /// <summary>Updates CurrentDevice and CurrentGamepadFamily from an active InputDevice.</summary>
    public void NotifyDeviceActivity(InputDevice device)
    {
        if (device == null)
            return;

        if (device is Gamepad)
        {
            SetActiveDevice(InputDeviceKind.Gamepad, DetectGamepadFamily(device));
        }
        else if (device is Keyboard || device is Mouse || device is Pointer)
        {
            SetActiveDevice(InputDeviceKind.KeyboardMouse, CurrentGamepadFamily);
        }
    }

    /// <summary>Directly applies a device/family state and fires change events only when modified.</summary>
    public void SetActiveDevice(InputDeviceKind deviceKind, GamepadFamily gamepadFamily = GamepadFamily.Generic)
    {
        bool kindChanged = CurrentDevice != deviceKind;
        bool familyChanged = deviceKind == InputDeviceKind.Gamepad && CurrentGamepadFamily != gamepadFamily;

        CurrentDevice = deviceKind;
        if (deviceKind == InputDeviceKind.Gamepad || gamepadFamily != GamepadFamily.Generic)
            CurrentGamepadFamily = gamepadFamily;

        if (familyChanged)
            GamepadFamilyChanged?.Invoke(CurrentGamepadFamily);

        if (kindChanged || familyChanged)
            DeviceChanged?.Invoke(CurrentDevice);
    }

    public static GamepadFamily DetectGamepadFamily(InputDevice device)
    {
        if (device == null)
            return GamepadFamily.Generic;

        if (device is DualShockGamepad)
            return GamepadFamily.PlayStation;

        if (device is XInputController)
            return GamepadFamily.Xbox;

        InputDeviceDescription desc = device.description;
        return DetectGamepadFamily(device.layout, desc.product, desc.manufacturer);
    }

    public static GamepadFamily DetectGamepadFamily(string layout, string product, string manufacturer)
    {
        if (ContainsAny(layout, product, manufacturer, "DualSense", "DualShock", "PlayStation", "Sony", "PS4", "PS5"))
            return GamepadFamily.PlayStation;

        if (ContainsAny(layout, product, manufacturer, "Xbox", "XInput", "Microsoft"))
            return GamepadFamily.Xbox;

        return GamepadFamily.Generic;
    }

    public static string FormatGamepadBindingLabel(string bindingPath, GamepadFamily family)
    {
        if (string.IsNullOrWhiteSpace(bindingPath))
            return string.Empty;

        string control = bindingPath.Trim();
        int slash = control.LastIndexOf('/');
        if (slash >= 0 && slash + 1 < control.Length)
            control = control.Substring(slash + 1);

        bool playStation = family == GamepadFamily.PlayStation;
        switch (control)
        {
            case "buttonSouth": return playStation ? "Cross" : "A";
            case "buttonEast": return playStation ? "Circle" : "B";
            case "buttonWest": return playStation ? "Square" : "X";
            case "buttonNorth": return playStation ? "Triangle" : "Y";
            case "leftShoulder": return playStation ? "L1" : "LB";
            case "rightShoulder": return playStation ? "R1" : "RB";
            case "leftTrigger": return playStation ? "L2" : "LT";
            case "rightTrigger": return playStation ? "R2" : "RT";
            case "start": return playStation ? "Options" : "Menu";
            case "select": return playStation ? "Share" : "View";
            case "leftStickPress": return "L3";
            case "rightStickPress": return "R3";
            default: return control;
        }
    }

    private static bool ContainsAny(string a, string b, string c, params string[] needles)
    {
        for (int i = 0; i < needles.Length; i++)
        {
            string needle = needles[i];
            if ((!string.IsNullOrEmpty(a) && a.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrEmpty(b) && b.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                || (!string.IsNullOrEmpty(c) && c.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
        }

        return false;
    }
}
