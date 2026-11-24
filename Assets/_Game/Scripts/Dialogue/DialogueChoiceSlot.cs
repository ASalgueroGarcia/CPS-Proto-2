using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DialogueChoiceSlot : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private TMP_Text optionText;
    [SerializeField] private GameObject selectionHighlight;

    private int _slotIndex;

    public void Setup(int index, string text)
    {
        _slotIndex = index;
        optionText.text = text;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(selected);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        DialogueManager.Instance?.SetSelectedChoiceIndex(_slotIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        
        DialogueManager.Instance?.SetSelectedChoiceIndex(_slotIndex);
        DialogueManager.Instance?.OnConfirmChoiceInput();
    }
}