using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Stylized non-1:1 keyboard and mouse diagram for the Settings keybinds page. Displays live GDD
/// bindings (WASD, LMB, RMB, Space/Shift, E, Esc) and highlights active keycaps and mouse buttons.
/// </summary>
public class KeyboardMouseDiagramView : MonoBehaviour
{
    [Header("Keycap Graphics")]
    [SerializeField] private Image keyEscGraphic;
    [SerializeField] private Image keyWGraphic;
    [SerializeField] private Image keyAGraphic;
    [SerializeField] private Image keySGraphic;
    [SerializeField] private Image keyDGraphic;
    [SerializeField] private Image keyEGraphic;
    [SerializeField] private Image keyShiftGraphic;
    [SerializeField] private Image keySpaceGraphic;
    [SerializeField] private Image mouseLeftGraphic;
    [SerializeField] private Image mouseRightGraphic;

    [Header("Legend Labels")]
    [SerializeField] private TMP_Text actionsText;
    [SerializeField] private TMP_Text inputsText;

    [Header("Feedback Settings")]
    [Min(1f)]
    [SerializeField] private float activeScaleMultiplier = 1.12f;
    [SerializeField] private Color idleKeyColor = new Color(0.28f, 0.21f, 0.17f, 1f);
    [SerializeField] private Color activeKeyColor = new Color(0.95f, 0.72f, 0.2f, 1f);

    private void OnEnable()
    {
        ApplyState(false, false, false, false, false, false, false, false, false, false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        bool esc = keyboard != null && keyboard.escapeKey.isPressed;
        bool w = keyboard != null && keyboard.wKey.isPressed;
        bool a = keyboard != null && keyboard.aKey.isPressed;
        bool s = keyboard != null && keyboard.sKey.isPressed;
        bool d = keyboard != null && keyboard.dKey.isPressed;
        bool e = keyboard != null && keyboard.eKey.isPressed;
        bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        bool space = keyboard != null && keyboard.spaceKey.isPressed;
        bool lmb = mouse != null && mouse.leftButton.isPressed;
        bool rmb = mouse != null && mouse.rightButton.isPressed;

        ApplyState(esc, w, a, s, d, e, shift, space, lmb, rmb);
    }

    /// <summary>Updates the GDD keyboard and mouse legend from live bindings.</summary>
    public void RefreshBindings(ControllerRebindService rebindService)
    {
        if (actionsText != null)
        {
            actionsText.text = "Move\nNormal Attack\nSpecial Attack\nDash\nInteract\nPause";
        }

        if (inputsText != null)
        {
            string attack = FormatKeyboardList(rebindService, "Attack", "LMB");
            string special = FormatKeyboardList(rebindService, "Special", "RMB");
            string dash = FormatKeyboardList(rebindService, "Dash", "Space / Shift");
            string interact = FormatKeyboardList(rebindService, "Interact", "E");
            string pause = FormatKeyboardList(rebindService, "PAUSE", "Esc");

            inputsText.text = $"W / A / S / D\n{attack}\n{special}\n{dash}\n{interact}\n{pause}";
        }
    }

    public void ApplyState(
        bool esc,
        bool w,
        bool a,
        bool s,
        bool d,
        bool e,
        bool shift,
        bool space,
        bool lmb,
        bool rmb)
    {
        ApplyKeyGraphic(keyEscGraphic, esc);
        ApplyKeyGraphic(keyWGraphic, w);
        ApplyKeyGraphic(keyAGraphic, a);
        ApplyKeyGraphic(keySGraphic, s);
        ApplyKeyGraphic(keyDGraphic, d);
        ApplyKeyGraphic(keyEGraphic, e);
        ApplyKeyGraphic(keyShiftGraphic, shift);
        ApplyKeyGraphic(keySpaceGraphic, space);
        ApplyKeyGraphic(mouseLeftGraphic, lmb);
        ApplyKeyGraphic(mouseRightGraphic, rmb);
    }

    private void ApplyKeyGraphic(Image graphic, bool active)
    {
        if (graphic == null)
            return;

        graphic.color = active ? activeKeyColor : idleKeyColor;
        float scale = active ? activeScaleMultiplier : 1f;
        graphic.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private static string FormatKeyboardList(ControllerRebindService service, string actionName, string fallback)
    {
        if (service == null)
            return fallback;

        List<string> paths = service.GetKeyboardMousePaths(actionName);
        if (paths.Count == 0 && actionName == "PAUSE")
            paths = service.GetKeyboardMousePaths("Pause");

        if (paths.Count == 0)
            return fallback;

        var labels = new List<string>(paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            string formatted = FormatKeyboardMousePath(paths[i]);
            if (!string.IsNullOrEmpty(formatted) && !labels.Contains(formatted))
                labels.Add(formatted);
        }

        return labels.Count > 0 ? string.Join(" / ", labels) : fallback;
    }

    private static string FormatKeyboardMousePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        string control = path.Trim();
        int slash = control.LastIndexOf('/');
        if (slash >= 0 && slash + 1 < control.Length)
            control = control.Substring(slash + 1);

        switch (control.ToLowerInvariant())
        {
            case "leftbutton": return "LMB";
            case "rightbutton": return "RMB";
            case "middlebutton": return "MMB";
            case "space": return "Space";
            case "shift":
            case "leftshift":
            case "rightshift": return "Shift";
            case "escape": return "Esc";
            case "e": return "E";
            default: return control.ToUpperInvariant();
        }
    }
}
