using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Interactive stylized gamepad graphic for the Settings keybinds page. Displays PlayStation or
/// Xbox glyphs, updates action callouts dynamically from ControllerRebindService, moves the left
/// thumbstick nub live, and lights up / scales matching controls and callouts on real input.
/// </summary>
public class ControllerVisualizerView : MonoBehaviour
{
    private const string MoveAction = "Move";
    private const string AttackAction = "Attack";
    private const string SpecialAction = "Special";
    private const string DashAction = "Dash";
    private const string InteractAction = "Interact";
    private const string PauseAction = "Pause";

    [Header("Stylized Controller Parts")]
    [SerializeField] private RectTransform leftStickNub;
    [SerializeField] private Image leftStickWellGraphic;
    [SerializeField] private Image leftStickNubGraphic;
    [SerializeField] private Image dpadHorizontalGraphic;
    [SerializeField] private Image dpadVerticalGraphic;
    [SerializeField] private Image buttonSouthGraphic;
    [SerializeField] private Image buttonEastGraphic;
    [SerializeField] private Image buttonWestGraphic;
    [SerializeField] private Image buttonNorthGraphic;
    [SerializeField] private Image leftShoulderGraphic;
    [SerializeField] private Image rightShoulderGraphic;
    [SerializeField] private Image leftTriggerGraphic;
    [SerializeField] private Image rightTriggerGraphic;
    [SerializeField] private Image startGraphic;

    [Header("Button Glyph Labels")]
    [SerializeField] private TMP_Text buttonSouthGlyph;
    [SerializeField] private TMP_Text buttonEastGlyph;
    [SerializeField] private TMP_Text buttonWestGlyph;
    [SerializeField] private TMP_Text buttonNorthGlyph;
    [SerializeField] private TMP_Text leftShoulderGlyph;
    [SerializeField] private TMP_Text rightShoulderGlyph;
    [SerializeField] private TMP_Text leftTriggerGlyph;
    [SerializeField] private TMP_Text rightTriggerGlyph;
    [SerializeField] private TMP_Text startGlyph;

    [Header("Action Callouts")]
    [SerializeField] private TMP_Text moveCallout;
    [SerializeField] private TMP_Text attackCallout;
    [SerializeField] private TMP_Text specialCallout;
    [SerializeField] private TMP_Text dashCallout;
    [SerializeField] private TMP_Text interactCallout;
    [SerializeField] private TMP_Text pauseCallout;

    [Header("Feedback Settings")]
    [Min(1f)]
    [SerializeField] private float stickTravelRadius = 14f;
    [Range(0.05f, 0.8f)]
    [SerializeField] private float stickDeadzone = 0.2f;
    [Range(0.05f, 0.8f)]
    [SerializeField] private float triggerThreshold = 0.2f;
    [Min(1f)]
    [SerializeField] private float activeScaleMultiplier = 1.18f;
    [Min(1f)]
    [SerializeField] private float activeCalloutScale = 1.08f;

    [Header("Palette")]
    [SerializeField] private Color idleControlColor = new Color(0.28f, 0.21f, 0.17f, 1f);
    [SerializeField] private Color idleStickWellColor = new Color(0.18f, 0.13f, 0.1f, 1f);
    [SerializeField] private Color activeControlColor = new Color(0.95f, 0.72f, 0.2f, 1f);
    [SerializeField] private Color idleCalloutColor = new Color(0.227f, 0.165f, 0.118f, 1f);
    [SerializeField] private Color activeCalloutColor = new Color(0.627f, 0.188f, 0.165f, 1f);
    [SerializeField] private Color listeningCalloutColor = new Color(0.76f, 0.45f, 0.08f, 1f);

    private readonly Dictionary<string, ControllerVisualControl> actionToControl =
        new Dictionary<string, ControllerVisualControl>(StringComparer.OrdinalIgnoreCase);

    private ControllerVisualInputState lastInputState;
    private GamepadFamily currentFamily = GamepadFamily.Xbox;
    private string listeningActionName;
    private Vector2 leftStickOrigin;
    private bool originCaptured;

    public Vector2 LeftStickNubOffset { get; private set; }
    public GamepadFamily CurrentFamily => currentFamily;

    private void Awake()
    {
        CaptureStickOrigin();
        ApplyDefaultMappings();
    }

    private void OnEnable()
    {
        CaptureStickOrigin();
        ApplyInputState(default);
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            ApplyInputState(ControllerVisualInputState.FromGamepad(gamepad));
        }
        else if (HasAnyActiveInput(lastInputState))
        {
            ApplyInputState(default);
        }
    }

    /// <summary>Updates button glyphs and dynamic action callouts from the current active bindings.</summary>
    public void RefreshBindings(
        ControllerRebindService rebindService,
        GamepadFamily family,
        string activeListeningAction = null)
    {
        currentFamily = family;
        listeningActionName = activeListeningAction;
        UpdateButtonFaceGlyphs(family);

        string movePath = ResolvePath(rebindService, MoveAction, "<Gamepad>/leftStick");
        string attackPath = ResolvePath(rebindService, AttackAction, "<Gamepad>/buttonSouth");
        string specialPath = ResolvePath(rebindService, SpecialAction, "<Gamepad>/buttonNorth");
        string dashPath = ResolvePath(rebindService, DashAction, "<Gamepad>/buttonEast");
        string interactPath = ResolvePath(rebindService, InteractAction, "<Gamepad>/buttonWest");
        string pausePath = ResolvePath(rebindService, PauseAction, "<Gamepad>/start");

        SetBinding(MoveAction, movePath, family, IsListening(MoveAction));
        SetBinding(AttackAction, attackPath, family, IsListening(AttackAction));
        SetBinding(SpecialAction, specialPath, family, IsListening(SpecialAction));
        SetBinding(DashAction, dashPath, family, IsListening(DashAction));
        SetBinding(InteractAction, interactPath, family, IsListening(InteractAction));
        SetBinding(PauseAction, pausePath, family, IsListening(PauseAction));

        ApplyInputState(lastInputState);
    }

    /// <summary>Binds a single action's callout label and active-highlight mapping directly.</summary>
    public void SetBinding(string actionName, string bindingPath, GamepadFamily family, bool isListening = false)
    {
        if (string.IsNullOrWhiteSpace(actionName))
            return;

        ControllerVisualControl mapped = MapBindingPathToControl(bindingPath);
        actionToControl[actionName] = mapped;

        TMP_Text label = GetCalloutLabel(actionName);
        if (label != null)
        {
            string glyph = isListening
                ? "..."
                : FormatCalloutGlyph(bindingPath, mapped, family);
            label.text = $"{FormatActionDisplayName(actionName)}: {glyph}";
        }
    }

    /// <summary>Applies a gamepad state snapshot to the stylized graphics, stick nub, and callouts.</summary>
    public void ApplyInputState(ControllerVisualInputState state)
    {
        CaptureStickOrigin();
        lastInputState = state;

        Vector2 clampedStick = Vector2.ClampMagnitude(state.leftStick, 1f);
        LeftStickNubOffset = clampedStick * stickTravelRadius;
        if (leftStickNub != null)
            leftStickNub.anchoredPosition = leftStickOrigin + LeftStickNubOffset;

        float leftStickStrength = state.GetActivationStrength(ControllerVisualControl.LeftStick, stickDeadzone, triggerThreshold);
        ApplyGraphicFeedback(leftStickNubGraphic, leftStickStrength, idleControlColor);
        if (leftStickWellGraphic != null)
            leftStickWellGraphic.color = Color.Lerp(idleStickWellColor, activeControlColor * 0.55f, leftStickStrength);

        float dpadStrength = state.GetActivationStrength(ControllerVisualControl.Dpad, stickDeadzone, triggerThreshold);
        ApplyGraphicFeedback(dpadHorizontalGraphic, dpadStrength, idleControlColor);
        ApplyGraphicFeedback(dpadVerticalGraphic, dpadStrength, idleControlColor);

        ApplyControlFeedback(buttonSouthGraphic, ControllerVisualControl.ButtonSouth, state);
        ApplyControlFeedback(buttonEastGraphic, ControllerVisualControl.ButtonEast, state);
        ApplyControlFeedback(buttonWestGraphic, ControllerVisualControl.ButtonWest, state);
        ApplyControlFeedback(buttonNorthGraphic, ControllerVisualControl.ButtonNorth, state);
        ApplyControlFeedback(leftShoulderGraphic, ControllerVisualControl.LeftShoulder, state);
        ApplyControlFeedback(rightShoulderGraphic, ControllerVisualControl.RightShoulder, state);
        ApplyControlFeedback(leftTriggerGraphic, ControllerVisualControl.LeftTrigger, state);
        ApplyControlFeedback(rightTriggerGraphic, ControllerVisualControl.RightTrigger, state);
        ApplyControlFeedback(startGraphic, ControllerVisualControl.Start, state);

        ApplyCalloutFeedback(MoveAction, moveCallout, state);
        ApplyCalloutFeedback(AttackAction, attackCallout, state);
        ApplyCalloutFeedback(SpecialAction, specialCallout, state);
        ApplyCalloutFeedback(DashAction, dashCallout, state);
        ApplyCalloutFeedback(InteractAction, interactCallout, state);
        ApplyCalloutFeedback(PauseAction, pauseCallout, state);
    }

    public bool IsControlHighlighted(ControllerVisualControl control)
    {
        return lastInputState.IsActive(control, stickDeadzone, triggerThreshold);
    }

    public bool IsActionHighlighted(string actionName)
    {
        ControllerVisualControl mapped = GetMappedControlForAction(actionName);
        return mapped != ControllerVisualControl.None && IsControlHighlighted(mapped);
    }

    public ControllerVisualControl GetMappedControlForAction(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName))
            return ControllerVisualControl.None;

        return actionToControl.TryGetValue(actionName, out ControllerVisualControl control)
            ? control
            : ControllerVisualControl.None;
    }

    /// <summary>Maps an Input System gamepad binding path to the corresponding visualizer control.</summary>
    public static ControllerVisualControl MapBindingPathToControl(string bindingPath)
    {
        if (string.IsNullOrWhiteSpace(bindingPath))
            return ControllerVisualControl.None;

        string control = bindingPath.Trim();
        int slash = control.LastIndexOf('/');
        if (slash >= 0 && slash + 1 < control.Length)
            control = control.Substring(slash + 1);

        if (control.Equals("leftStick", StringComparison.OrdinalIgnoreCase)
            || control.Equals("leftStickPress", StringComparison.OrdinalIgnoreCase))
        {
            return ControllerVisualControl.LeftStick;
        }

        if (control.StartsWith("dpad", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.Dpad;

        if (control.Equals("buttonSouth", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.ButtonSouth;
        if (control.Equals("buttonEast", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.ButtonEast;
        if (control.Equals("buttonWest", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.ButtonWest;
        if (control.Equals("buttonNorth", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.ButtonNorth;
        if (control.Equals("leftShoulder", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.LeftShoulder;
        if (control.Equals("rightShoulder", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.RightShoulder;
        if (control.Equals("leftTrigger", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.LeftTrigger;
        if (control.Equals("rightTrigger", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.RightTrigger;
        if (control.Equals("start", StringComparison.OrdinalIgnoreCase))
            return ControllerVisualControl.Start;

        return ControllerVisualControl.None;
    }

    /// <summary>Compact glyph text rendered directly onto the stylized controller buttons.</summary>
    public static string FormatShortFaceGlyph(ControllerVisualControl control, GamepadFamily family)
    {
        bool playStation = family == GamepadFamily.PlayStation;
        switch (control)
        {
            case ControllerVisualControl.ButtonSouth: return playStation ? "X" : "A";
            case ControllerVisualControl.ButtonEast: return playStation ? "O" : "B";
            case ControllerVisualControl.ButtonWest: return playStation ? "SQ" : "X";
            case ControllerVisualControl.ButtonNorth: return playStation ? "TR" : "Y";
            case ControllerVisualControl.LeftShoulder: return playStation ? "L1" : "LB";
            case ControllerVisualControl.RightShoulder: return playStation ? "R1" : "RB";
            case ControllerVisualControl.LeftTrigger: return playStation ? "L2" : "LT";
            case ControllerVisualControl.RightTrigger: return playStation ? "R2" : "RT";
            case ControllerVisualControl.Start: return playStation ? "OPT" : "MENU";
            case ControllerVisualControl.LeftStick: return "LS";
            case ControllerVisualControl.Dpad: return "D-PAD";
            default: return string.Empty;
        }
    }

    private void UpdateButtonFaceGlyphs(GamepadFamily family)
    {
        SetText(buttonSouthGlyph, FormatShortFaceGlyph(ControllerVisualControl.ButtonSouth, family));
        SetText(buttonEastGlyph, FormatShortFaceGlyph(ControllerVisualControl.ButtonEast, family));
        SetText(buttonWestGlyph, FormatShortFaceGlyph(ControllerVisualControl.ButtonWest, family));
        SetText(buttonNorthGlyph, FormatShortFaceGlyph(ControllerVisualControl.ButtonNorth, family));
        SetText(leftShoulderGlyph, FormatShortFaceGlyph(ControllerVisualControl.LeftShoulder, family));
        SetText(rightShoulderGlyph, FormatShortFaceGlyph(ControllerVisualControl.RightShoulder, family));
        SetText(leftTriggerGlyph, FormatShortFaceGlyph(ControllerVisualControl.LeftTrigger, family));
        SetText(rightTriggerGlyph, FormatShortFaceGlyph(ControllerVisualControl.RightTrigger, family));
        SetText(startGlyph, FormatShortFaceGlyph(ControllerVisualControl.Start, family));
    }

    private void ApplyControlFeedback(Image graphic, ControllerVisualControl control, ControllerVisualInputState state)
    {
        float strength = state.GetActivationStrength(control, stickDeadzone, triggerThreshold);
        ApplyGraphicFeedback(graphic, strength, idleControlColor);
    }

    private void ApplyGraphicFeedback(Image graphic, float strength, Color baseColor)
    {
        if (graphic == null)
            return;

        graphic.color = Color.Lerp(baseColor, activeControlColor, strength);
        float scale = Mathf.Lerp(1f, activeScaleMultiplier, strength);
        graphic.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private void ApplyCalloutFeedback(string actionName, TMP_Text label, ControllerVisualInputState state)
    {
        if (label == null)
            return;

        if (IsListening(actionName))
        {
            label.color = listeningCalloutColor;
            label.rectTransform.localScale = new Vector3(activeCalloutScale, activeCalloutScale, 1f);
            return;
        }

        ControllerVisualControl mapped = GetMappedControlForAction(actionName);
        float strength = state.GetActivationStrength(mapped, stickDeadzone, triggerThreshold);
        label.color = Color.Lerp(idleCalloutColor, activeCalloutColor, strength);
        float scale = Mathf.Lerp(1f, activeCalloutScale, strength);
        label.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private void ApplyDefaultMappings()
    {
        actionToControl[MoveAction] = ControllerVisualControl.LeftStick;
        actionToControl[AttackAction] = ControllerVisualControl.ButtonSouth;
        actionToControl[SpecialAction] = ControllerVisualControl.ButtonNorth;
        actionToControl[DashAction] = ControllerVisualControl.ButtonEast;
        actionToControl[InteractAction] = ControllerVisualControl.ButtonWest;
        actionToControl[PauseAction] = ControllerVisualControl.Start;
    }

    private void CaptureStickOrigin()
    {
        if (originCaptured || leftStickNub == null)
            return;

        leftStickOrigin = leftStickNub.anchoredPosition;
        originCaptured = true;
    }

    private bool IsListening(string actionName)
    {
        return !string.IsNullOrEmpty(listeningActionName)
               && string.Equals(listeningActionName, actionName, StringComparison.OrdinalIgnoreCase);
    }

    private TMP_Text GetCalloutLabel(string actionName)
    {
        if (actionName.Equals(MoveAction, StringComparison.OrdinalIgnoreCase)) return moveCallout;
        if (actionName.Equals(AttackAction, StringComparison.OrdinalIgnoreCase)) return attackCallout;
        if (actionName.Equals(SpecialAction, StringComparison.OrdinalIgnoreCase)) return specialCallout;
        if (actionName.Equals(DashAction, StringComparison.OrdinalIgnoreCase)) return dashCallout;
        if (actionName.Equals(InteractAction, StringComparison.OrdinalIgnoreCase)) return interactCallout;
        if (actionName.Equals(PauseAction, StringComparison.OrdinalIgnoreCase)) return pauseCallout;
        return null;
    }

    private static string ResolvePath(ControllerRebindService rebindService, string actionName, string fallback)
    {
        if (rebindService == null)
            return fallback;

        string path = rebindService.GetEffectiveGamepadPath(actionName);
        if (string.IsNullOrWhiteSpace(path) && actionName.Equals(PauseAction, StringComparison.OrdinalIgnoreCase))
            path = rebindService.GetEffectiveGamepadPath("PAUSE");

        return string.IsNullOrWhiteSpace(path) ? fallback : path;
    }

    private static string FormatCalloutGlyph(string bindingPath, ControllerVisualControl mapped, GamepadFamily family)
    {
        if (mapped == ControllerVisualControl.LeftStick)
            return "Left Stick";

        if (mapped == ControllerVisualControl.Dpad)
            return "D-Pad";

        string formatted = InputDeviceTracker.FormatGamepadBindingLabel(bindingPath, family);
        return string.IsNullOrWhiteSpace(formatted) ? "-" : formatted;
    }

    private static string FormatActionDisplayName(string actionName)
    {
        if (actionName.Equals(MoveAction, StringComparison.OrdinalIgnoreCase)) return "Move";
        if (actionName.Equals(AttackAction, StringComparison.OrdinalIgnoreCase)) return "Attack";
        if (actionName.Equals(SpecialAction, StringComparison.OrdinalIgnoreCase)) return "Special";
        if (actionName.Equals(DashAction, StringComparison.OrdinalIgnoreCase)) return "Dash";
        if (actionName.Equals(InteractAction, StringComparison.OrdinalIgnoreCase)) return "Interact";
        if (actionName.Equals(PauseAction, StringComparison.OrdinalIgnoreCase)) return "Pause";
        return actionName;
    }

    private static bool HasAnyActiveInput(ControllerVisualInputState state)
    {
        return state.leftStick.sqrMagnitude > 0.0001f
               || state.dpad.sqrMagnitude > 0.0001f
               || state.leftTrigger > 0.0001f
               || state.rightTrigger > 0.0001f
               || state.buttonSouth
               || state.buttonEast
               || state.buttonWest
               || state.buttonNorth
               || state.leftShoulder
               || state.rightShoulder
               || state.start;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
