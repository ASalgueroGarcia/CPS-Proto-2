using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Node : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private const float FillScale = 0.82f;

    public int Row { get; private set; }
    public int Column { get; private set; }
    public NodeTypeEnum Type { get; private set; }

    private Button button;
    private Image ring;
    private Image fill;
    private TMP_Text icon;
    private TMP_Text label;
    private Action<Node> onInspect;

    public static Color GetRoomColor(NodeTypeEnum type)
    {
        switch (type)
        {
            case NodeTypeEnum.Combat:
                return new Color(0.85f, 0.25f, 0.25f);
            case NodeTypeEnum.MoreHealth:
                return new Color(0.25f, 0.75f, 0.40f);
            case NodeTypeEnum.MoreButtons:
                return new Color(0.95f, 0.75f, 0.15f);
            case NodeTypeEnum.Shop:
                return new Color(0.20f, 0.60f, 0.95f);
            case NodeTypeEnum.Treasure:
                return new Color(0.65f, 0.35f, 0.85f);
            case NodeTypeEnum.FinalFight:
                return new Color(0.90f, 0.15f, 0.30f);
            default:
                return Color.white;
        }
    }

    public void Init(int row, int column, NodeTypeEnum type, Action<Node> clickCallback, Action<Node> inspectCallback)
    {
        Row = row;
        Column = column;
        Type = type;
        onInspect = inspectCallback;

        ring = GetComponent<Image>();
        float size = ((RectTransform)transform).sizeDelta.y;

        fill = AddImage("Fill", ring.sprite, size * FillScale);
        icon = AddText("Icon", IconFor(type), size * 0.5f, Vector2.zero, new Vector2(size, size));
        icon.alignment = TextAlignmentOptions.Midline;

        label = AddText("Label", "YOU ARE HERE", size * 0.22f, new Vector2(0f, -size * 0.8f), new Vector2(size * 3f, size * 0.4f));
        label.characterSpacing = 6f;

        button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => clickCallback(this));
    }

    public void SetLook(Color fillColor, Color iconColor, Color ringColor, bool showRing, bool interactable, bool current)
    {
        ring.color = showRing ? ringColor : Color.clear;
        fill.color = fillColor;
        icon.color = iconColor;
        label.color = ringColor;
        label.gameObject.SetActive(current);
        button.interactable = interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onInspect?.Invoke(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        onInspect?.Invoke(this);
    }

    private static string IconFor(NodeTypeEnum type)
    {
        switch (type)
        {
            case NodeTypeEnum.Combat:
                return "×";
            case NodeTypeEnum.MoreHealth:
                return "+";
            case NodeTypeEnum.MoreButtons:
                return "¤";
            case NodeTypeEnum.Shop:
                return "▲";
            case NodeTypeEnum.Treasure:
                return "■";
            case NodeTypeEnum.FinalFight:
                return "♣";
            default:
                return "?";
        }
    }

    private Image AddImage(string name, Sprite sprite, float size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.sizeDelta = Vector2.one * size;

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text AddText(string name, string text, float fontSize, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }
}
