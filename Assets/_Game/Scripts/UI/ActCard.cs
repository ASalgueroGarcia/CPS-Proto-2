using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One act on the act select poster. Paints itself for one of the four states from the
/// design sheet (GDD > Interface > HUD > Poster): Selected, Available, Locked, Finished.
/// </summary>
public class ActCard : MonoBehaviour
{
    public enum State { Selected, Available, Locked, Finished }

    [Header("Parts")]
    [SerializeField] private Image paper;
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private GameObject lockedBand;
    [Tooltip("Numeral, dividers, emblem frame and emblem - red on an open act, grey when locked.")]
    [SerializeField] private Graphic[] accentGraphics;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI subtitle;

    [Header("Scene Progress")]
    [SerializeField] private Image[] pipFrames;
    [SerializeField] private Image[] pipFills;
    [SerializeField] private TextMeshProUGUI sceneLabel;

    [Header("Cue Strip")]
    [SerializeField] private GameObject cueStrip;
    [SerializeField] private Image cueFill;
    [SerializeField] private Image[] cueFrame;
    [SerializeField] private TextMeshProUGUI cueLabel;

    [Header("Colors")]
    [SerializeField] private Color paperColor = new Color32(0xED, 0xE2, 0xCB, 0xFF);
    [SerializeField] private Color selectedPaperColor = new Color32(0xF7, 0xED, 0xD8, 0xFF);
    [SerializeField] private Color lockedPaperColor = new Color32(0xDD, 0xD5, 0xC6, 0xFF);
    [SerializeField] private Color accentColor = new Color32(0x94, 0x3A, 0x30, 0xFF);
    [SerializeField] private Color inkColor = new Color32(0x2B, 0x25, 0x22, 0xFF);
    [SerializeField] private Color mutedInkColor = new Color32(0x7A, 0x70, 0x66, 0xFF);
    [SerializeField] private Color lockedColor = new Color32(0xA3, 0x9A, 0x8E, 0xFF);
    [SerializeField] private Color quietCueColor = new Color32(0xA3, 0x9A, 0x8E, 0xFF);

    public void Show(State state, int scenesCleared, int scenesTotal)
    {
        bool locked = state == State.Locked;
        bool selected = state == State.Selected;

        paper.color = selected ? selectedPaperColor : locked ? lockedPaperColor : paperColor;
        selectedBorder.SetActive(selected);
        lockedBand.SetActive(locked);

        Color accent = locked ? lockedColor : accentColor;
        foreach (Graphic graphic in accentGraphics)
        {
            graphic.color = accent;
        }

        title.color = locked ? lockedColor : inkColor;
        subtitle.color = locked ? lockedColor : mutedInkColor;

        for (int i = 0; i < pipFrames.Length; i++)
        {
            pipFrames[i].color = accent;
            pipFills[i].color = i < scenesCleared ? accent : paper.color;
        }

        int currentScene = Mathf.Clamp(scenesCleared + 1, 1, scenesTotal);
        sceneLabel.text = $"SCENE {currentScene} OF {scenesTotal}";
        sceneLabel.color = locked ? lockedColor : inkColor;

        ShowCue(state, scenesCleared >= scenesTotal);
    }

    private void ShowCue(State state, bool finished)
    {
        cueStrip.SetActive(state != State.Locked);

        Color frame;
        Color fill;
        Color label;

        if (state == State.Selected)
        {
            frame = accentColor;
            fill = accentColor;
            label = Color.white;
            cueLabel.text = finished ? "PLAY AGAIN    E" : "TAKE YOUR SEAT    E";
        }
        else if (state == State.Finished)
        {
            frame = accentColor;
            fill = Color.clear;
            label = accentColor;
            cueLabel.text = "PLAY AGAIN    E";
        }
        else
        {
            frame = quietCueColor;
            fill = Color.clear;
            label = quietCueColor;
            cueLabel.text = "SELECT    ←  →";
        }

        cueFill.color = fill;
        cueLabel.color = label;
        foreach (Image side in cueFrame)
        {
            side.color = frame;
        }
    }
}
