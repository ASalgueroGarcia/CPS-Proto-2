using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Frame snapshot of live gamepad inputs fed into ControllerVisualizerView.
/// </summary>
public struct ControllerVisualInputState
{
    public Vector2 leftStick;
    public Vector2 dpad;
    public float leftTrigger;
    public float rightTrigger;
    public bool buttonSouth;
    public bool buttonEast;
    public bool buttonWest;
    public bool buttonNorth;
    public bool leftShoulder;
    public bool rightShoulder;
    public bool start;

    public static ControllerVisualInputState FromGamepad(Gamepad gamepad)
    {
        if (gamepad == null)
            return default;

        return new ControllerVisualInputState
        {
            leftStick = gamepad.leftStick.ReadValue(),
            dpad = gamepad.dpad.ReadValue(),
            leftTrigger = gamepad.leftTrigger.ReadValue(),
            rightTrigger = gamepad.rightTrigger.ReadValue(),
            buttonSouth = gamepad.buttonSouth.isPressed,
            buttonEast = gamepad.buttonEast.isPressed,
            buttonWest = gamepad.buttonWest.isPressed,
            buttonNorth = gamepad.buttonNorth.isPressed,
            leftShoulder = gamepad.leftShoulder.isPressed,
            rightShoulder = gamepad.rightShoulder.isPressed,
            start = gamepad.startButton.isPressed
        };
    }

    public bool IsActive(ControllerVisualControl control, float stickDeadzone = 0.2f, float triggerThreshold = 0.2f)
    {
        return GetActivationStrength(control, stickDeadzone, triggerThreshold) > 0f;
    }

    public float GetActivationStrength(ControllerVisualControl control, float stickDeadzone = 0.2f, float triggerThreshold = 0.2f)
    {
        switch (control)
        {
            case ControllerVisualControl.LeftStick:
                float stickMag = leftStick.magnitude;
                return stickMag >= stickDeadzone ? Mathf.Clamp01(stickMag) : 0f;
            case ControllerVisualControl.Dpad:
                return dpad.sqrMagnitude >= 0.25f ? 1f : 0f;
            case ControllerVisualControl.ButtonSouth:
                return buttonSouth ? 1f : 0f;
            case ControllerVisualControl.ButtonEast:
                return buttonEast ? 1f : 0f;
            case ControllerVisualControl.ButtonWest:
                return buttonWest ? 1f : 0f;
            case ControllerVisualControl.ButtonNorth:
                return buttonNorth ? 1f : 0f;
            case ControllerVisualControl.LeftShoulder:
                return leftShoulder ? 1f : 0f;
            case ControllerVisualControl.RightShoulder:
                return rightShoulder ? 1f : 0f;
            case ControllerVisualControl.LeftTrigger:
                return leftTrigger >= triggerThreshold ? Mathf.Clamp01(leftTrigger) : 0f;
            case ControllerVisualControl.RightTrigger:
                return rightTrigger >= triggerThreshold ? Mathf.Clamp01(rightTrigger) : 0f;
            case ControllerVisualControl.Start:
                return start ? 1f : 0f;
            default:
                return 0f;
        }
    }
}
