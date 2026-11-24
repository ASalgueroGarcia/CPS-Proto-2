using System;
using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class ConversationSignView : MonoBehaviour
{
    [Header("Referencias del Prefab")]
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject advanceMark;

    [Header("Animación y Escritura")]
    [SerializeField] private float dropOffsetY = 180f;
    [SerializeField] private float dropDuration = 0.35f;
    [SerializeField] private float riseDuration = 0.25f;
    [SerializeField] private float charsPerSecond = 35f;

    [Header("Choices UI Settings")]
    [SerializeField] private GameObject choicesContainer;
    [SerializeField] private DialogueChoiceSlot[] choiceSlots;

    private CanvasGroup _canvasGroup;
    private Tween _moveTween;
    private Tween _textTween;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (advanceMark != null) advanceMark.SetActive(false);
        HideChoicesUI();
    }

    public void DropAndShow(string text)
    {
        HideChoicesUI();
        SetTalkingState(true);
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;

        _moveTween?.Kill();
        visualRoot.anchoredPosition = Vector2.zero;

        // Estado A: baja el cartel e ignora input hasta aterrizar
        _moveTween = visualRoot.DOAnchorPosY(dropOffsetY, dropDuration)
            .From()
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                DialogueManager.Instance?.NotifySignLanded();
                PrintText(text);
            });
    }

    public void ShowNewLine(string text)
    {
        // El cartel ya estaba bajado: no se mueve, solo se ilumina y cambia el texto
        SetTalkingState(true);
        PrintText(text);
    }

    private void PrintText(string text)
    {
        HideChoicesUI();
        _textTween?.Kill();
        if (advanceMark != null) advanceMark.SetActive(false);

        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;

        float duration = Mathf.Max(0.05f, text.Length / charsPerSecond);
        _textTween = DOTween.To(
                () => dialogueText.maxVisibleCharacters,
                x => dialogueText.maxVisibleCharacters = x,
                text.Length,
                duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (advanceMark != null) advanceMark.SetActive(true);
                DialogueManager.Instance?.NotifyTextFinishedPrinting();
            });
    }

    public void CompleteInstant()
    {
        _textTween?.Kill();
        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
        if (advanceMark != null) advanceMark.SetActive(true);
    }

    public void SetTalkingState(bool isTalking)
    {
        // Regla GDD: el hablante inactivo conserva su última línea al 50% de opacidad
        _canvasGroup.alpha = isTalking ? 1f : 0.5f;
        if (!isTalking && advanceMark != null)
        {
            advanceMark.SetActive(false);
        }
    }

    public void RiseAndDestroy(Action onFinished)
    {
        _moveTween?.Kill();
        _textTween?.Kill();
        if (advanceMark != null) advanceMark.SetActive(false);

        _moveTween = visualRoot.DOAnchorPosY(dropOffsetY, riseDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                onFinished?.Invoke();
                Destroy(gameObject);
            });
    }

    private void HideChoicesUI()
    {
        if (choicesContainer != null) choicesContainer.SetActive(false);
        if (dialogueText != null) dialogueText.gameObject.SetActive(true);
    }

    public void ShowChoices(List<string> optionTexts, bool needsDrop)
    {
        SetTalkingState(true);

        if (advanceMark != null) advanceMark.SetActive(false);
        if (dialogueText != null) dialogueText.gameObject.SetActive(false);
        if (choicesContainer != null) choicesContainer.SetActive(true);

        for (int i = 0; i < choiceSlots.Length; i++)
        {
            if (i < optionTexts.Count) {
                choiceSlots[i].gameObject.SetActive(true);
                choiceSlots[i].Setup(i, optionTexts[i]);
            }
            else
                choiceSlots[i].gameObject.SetActive(false);
        }

        if (needsDrop)
        {
            _moveTween?.Kill();
            visualRoot.anchoredPosition = Vector2.zero;
            _moveTween = visualRoot.DOAnchorPosY(dropOffsetY, dropDuration)
                .From()
                .SetEase(Ease.OutBack)
                .OnComplete(() => DialogueManager.Instance?.NotifySignLanded());
        }
    }

    public void UpdateChoiceSelection(int selectedIndex)
    {
        if (choiceSlots == null) return;
        for (int i = 0; i < choiceSlots.Length; i++)
        {
            if (choiceSlots[i].gameObject.activeSelf)
                choiceSlots[i].SetSelected(i == selectedIndex);
        }
    }

    public void FreezeChosenText(string chosenText)
    {
        HideChoicesUI();
        dialogueText.text = chosenText;
        dialogueText.ForceMeshUpdate();
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;
    }
}
