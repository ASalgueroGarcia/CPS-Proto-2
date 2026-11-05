using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Utilities;

/// <summary>
/// Shows a button prompt for the device the player used last (GDD, HUD states: "auto-swap to last
/// used device"). The text set in the prefab is the keyboard / mouse prompt.
/// </summary>
public class InputPromptLabel : MonoBehaviour
{
    public enum PadButton
    {
        North,
        West
    }

    private static bool _usingGamepad;

    private TMP_Text _text;
    private string _keyboardPrompt;
    private PadButton _button;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void TrackLastDevice()
    {
        InputSystem.onAnyButtonPress.Call(control => _usingGamepad = control.device is Gamepad);
    }

    /// <summary>Adds the swap to the text under <paramref name="prompt"/>.</summary>
    public static void AddTo(GameObject prompt, PadButton button = PadButton.North)
    {
        if (prompt == null) return;
        TMP_Text text = prompt.GetComponentInChildren<TMP_Text>(true);
        if (text != null && !text.TryGetComponent(out InputPromptLabel _))
        {
            text.gameObject.AddComponent<InputPromptLabel>()._button = button;
        }
    }

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _keyboardPrompt = _text.text;
    }

    private void Update()
    {
        // A stick has no press to catch, and walking up to a prompt usually means only the stick moved.
        Gamepad gamepad = Gamepad.current;
        if (gamepad != null && gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f) _usingGamepad = true;

        string prompt = _usingGamepad ? GamepadPrompt(gamepad) : _keyboardPrompt;
        if (_text.text != prompt) _text.text = prompt;
    }

    private string GamepadPrompt(Gamepad gamepad)
    {
        string xbox = _button == PadButton.North ? "Y" : "X";
        string playStation = _button == PadButton.North ? "△" : "□";
        return gamepad is DualShockGamepad && _text.font.HasCharacter(playStation[0], true) ? playStation : xbox;
    }
}
