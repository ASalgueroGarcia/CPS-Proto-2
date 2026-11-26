using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Node : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private const float FillScale = 0.82f;

    [Header("Prefab Visuals")]
    [SerializeField] private Image ring;
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text icon;
    [SerializeField] private TMP_Text label;

    public int Row { get; private set; }
    public int Column { get; private set; }
    public NodeTypeEnum Type { get; private set; }
    public Image Ring => ring;
    public Image Fill => fill;
    public TMP_Text Icon => icon;
    public TMP_Text Label => label;

    private Button button;
    private Action<Node> onInspect;

    public static Color GetRoomColor(NodeTypeEnum type)
    {
        switch (type)
        {
            case NodeTypeEnum.Combat:
                return new Color(0.85f, 0.25f, 0.25f);
            case NodeTypeEnum.Health:
                return new Color(0.25f, 0.75f, 0.40f);
            case NodeTypeEnum.Shop:
                return new Color(0.20f, 0.60f, 0.95f);
            case NodeTypeEnum.Treasure:
                return new Color(0.65f, 0.35f, 0.85f);
            case NodeTypeEnum.Elite:
                return new Color(0.95f, 0.50f, 0.15f);
            case NodeTypeEnum.Mystery:
                return new Color(0.90f, 0.78f, 0.25f);
            case NodeTypeEnum.FinalFight:
                return new Color(0.90f, 0.15f, 0.30f);
            default:
                return Color.white;
        }
    }

    public static string IconFor(NodeTypeEnum type)
    {
        switch (type)
        {
            case NodeTypeEnum.Combat:
                return "×";
            case NodeTypeEnum.Health:
                return "+";
            case NodeTypeEnum.Shop:
                return "▲";
            case NodeTypeEnum.Treasure:
                return "■";
            case NodeTypeEnum.Elite:
                return "♦";
            case NodeTypeEnum.Mystery:
                return "?";
            case NodeTypeEnum.FinalFight:
                return "♣";
            default:
                return "?";
        }
    }

    public void Init(int row, int column, NodeTypeEnum type, Action<Node> clickCallback, Action<Node> inspectCallback)
    {
        Row = row;
        Column = column;
        Type = type;
        onInspect = inspectCallback;

        if (ring == null) ring = GetComponent<Image>();
        float size = ((RectTransform)transform).sizeDelta.y;
        if (size <= 0f) size = 64f;

        if (fill != null)
        {
            if (fill.sprite == null && ring != null) fill.sprite = ring.sprite;
            fill.rectTransform.sizeDelta = Vector2.one * (size * FillScale);
            fill.raycastTarget = false;
        }

        if (icon != null)
        {
            icon.text = IconFor(type);
            icon.fontSize = size * 0.5f;
            icon.rectTransform.sizeDelta = new Vector2(size, size);
            icon.alignment = TextAlignmentOptions.Midline;
            icon.raycastTarget = false;
        }

        if (label != null)
        {
            label.text = "YOU ARE HERE";
            label.fontSize = size * 0.22f;
            label.characterSpacing = 6f;
            label.rectTransform.anchoredPosition = new Vector2(0f, -size * 0.8f);
            label.rectTransform.sizeDelta = new Vector2(size * 3f, size * 0.4f);
            label.raycastTarget = false;
        }

        button = GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clickCallback?.Invoke(this));
    }

    public void SetLook(Color fillColor, Color iconColor, Color ringColor, bool showRing, bool interactable, bool current)
    {
        if (ring != null) ring.color = showRing ? ringColor : Color.clear;
        if (fill != null) fill.color = fillColor;
        if (icon != null) icon.color = iconColor;
        if (label != null)
        {
            label.color = ringColor;
            label.gameObject.SetActive(current);
        }

        if (button != null) button.interactable = interactable;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onInspect?.Invoke(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        onInspect?.Invoke(this);
    }
}
