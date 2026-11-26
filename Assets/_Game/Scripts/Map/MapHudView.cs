using TMPro;
using UnityEngine;

public class MapHudView : MonoBehaviour
{
    [Header("HUD Text References (optional)")]
    [SerializeField] private TMP_Text header;
    [SerializeField] private TMP_Text statsHud;
    [SerializeField] private TMP_Text inspect;
    [SerializeField] private TMP_Text legend;

    [Header("Colors")]
    [SerializeField] private Color legendColor = new Color(0.65f, 0.65f, 0.65f);

    public TMP_Text Header => header;
    public TMP_Text StatsHud => statsHud;
    public TMP_Text Inspect => inspect;
    public TMP_Text Legend => legend;

    public void EnsureBuilt(Transform canvasRoot)
    {
        if (canvasRoot == null) return;

        if (header == null)
            header = MakeText(canvasRoot, "Header", new Vector2(0f, 1f), TextAlignmentOptions.TopLeft, 34f, new Vector2(560f, 120f));

        if (statsHud == null)
            statsHud = MakeText(canvasRoot, "Hud", new Vector2(1f, 1f), TextAlignmentOptions.TopRight, 34f, new Vector2(560f, 120f));

        if (inspect == null)
        {
            inspect = MakeText(canvasRoot, "Inspect", new Vector2(0.5f, 0f), TextAlignmentOptions.Bottom, 30f, new Vector2(1200f, 100f));
            inspect.rectTransform.anchoredPosition += Vector2.up * 68f;
        }

        if (legend == null)
        {
            legend = MakeText(canvasRoot, "Legend", new Vector2(0.5f, 0f), TextAlignmentOptions.Bottom, 20f, new Vector2(1400f, 80f));
            legend.color = legendColor;
        }

        legend.text = FormatLegend();
    }

    public void Refresh(int actNumber, int sceneNumber, int takeNumber, float currentHealth, float maxHealth, int coins)
    {
        if (header != null) header.text = FormatHeader(actNumber, sceneNumber, takeNumber);
        if (statsHud != null) statsHud.text = FormatStats(currentHealth, maxHealth, coins);
        if (inspect != null) inspect.text = string.Empty;
        if (legend != null) legend.text = FormatLegend();
    }

    public void ShowInspect(int row, NodeTypeEnum type)
    {
        if (inspect != null) inspect.text = FormatNodeDescription(row, type);
    }

    public static string FormatHeader(int actNumber, int sceneNumber, int takeNumber)
    {
        return $"ACT {actNumber} · SCENE {sceneNumber}\n<size=70%>Take {takeNumber}</size>";
    }

    public static string FormatStats(float currentHealth, float maxHealth, int coins)
    {
        string health = maxHealth > 0f ? $"{currentHealth:0} / {maxHealth:0}" : "-- / --";
        return $"HP {health}\n<size=70%>Buttons {coins}</size>";
    }

    public static string FormatNodeDescription(int row, NodeTypeEnum type)
    {
        if (row == 0) return "First Spot · 4-5 rooms";

        switch (type)
        {
            case NodeTypeEnum.Combat:
                return "Fight · 4-5 rooms";
            case NodeTypeEnum.Health:
                return "Health · +60 HP rest room";
            case NodeTypeEnum.Shop:
                return "Shop · merchant";
            case NodeTypeEnum.Treasure:
                return "Treasure · +50 HP or Buttons";
            case NodeTypeEnum.Elite:
                return "Elite · high-risk HL3 fight";
            case NodeTypeEnum.Mystery:
                return "Mystery · unknown encounter";
            case NodeTypeEnum.FinalFight:
                return "Final Fight · Boss";
            default:
                return string.Empty;
        }
    }

    public static string FormatLegend()
    {
        return $"{Node.IconFor(NodeTypeEnum.Combat)} Fight  ·  "
             + $"{Node.IconFor(NodeTypeEnum.Health)} Health  ·  "
             + $"{Node.IconFor(NodeTypeEnum.Shop)} Shop  ·  "
             + $"{Node.IconFor(NodeTypeEnum.Treasure)} Treasure  ·  "
             + $"{Node.IconFor(NodeTypeEnum.Elite)} Elite  ·  "
             + $"{Node.IconFor(NodeTypeEnum.Mystery)} Mystery  ·  "
             + $"{Node.IconFor(NodeTypeEnum.FinalFight)} Final Fight";
    }

    private static TMP_Text MakeText(Transform parent, string name, Vector2 corner, TextAlignmentOptions alignment, float size, Vector2 boxSize)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = new Vector2((0.5f - corner.x) * 80f, (0.5f - corner.y) * 60f);
        rect.sizeDelta = boxSize;

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }
}
