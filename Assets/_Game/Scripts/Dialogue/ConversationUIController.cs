using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConversationUIController : MonoBehaviour
{
    [Serializable]
    public struct SpeakerAnchor
    {
        public Speaker speaker;
        public Transform worldAnchor;
    }

    [Header("Prefab y Contenedor")]
    [SerializeField] private ConversationSignView signPrefab;
    [SerializeField] private RectTransform signsContainer;
    [SerializeField] private Camera mainCamera;

    [Header("Puntos de Anclaje en Escena")]
    [SerializeField] private List<SpeakerAnchor> speakerAnchors = new List<SpeakerAnchor>();

    [Header("Opcional: Barra de progreso Skip (1s)")]
    [SerializeField] private Image skipFillImage;

    private readonly Dictionary<Speaker, ConversationSignView> _activeSigns = new Dictionary<Speaker, ConversationSignView>();
    private readonly Dictionary<Speaker, Transform> _anchorLookup = new Dictionary<Speaker, Transform>();
    private Speaker _currentSpeaker;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        foreach (var item in speakerAnchors)
        {
            if (item.worldAnchor != null) _anchorLookup[item.speaker] = item.worldAnchor;
        }

        if (skipFillImage != null) skipFillImage.fillAmount = 0f;
    }

    private void Start()
    {
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.OnLineStarted += HandleLineStarted;
        DialogueManager.Instance.OnCompleteLineInstantly += HandleCompleteInstant;
        DialogueManager.Instance.OnSkipProgressChanged += HandleSkipProgress;
        DialogueManager.Instance.OnConversationEnding += HandleConversationEnding;
        DialogueManager.Instance.OnChoicesPresented += HandleChoicesPresented;
        DialogueManager.Instance.OnChoiceSelectionChanged += HandleChoiceSelectionChanged;
        DialogueManager.Instance.OnChoiceSelected += HandleChoiceSelected;
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.OnLineStarted -= HandleLineStarted;
        DialogueManager.Instance.OnCompleteLineInstantly -= HandleCompleteInstant;
        DialogueManager.Instance.OnSkipProgressChanged -= HandleSkipProgress;
        DialogueManager.Instance.OnConversationEnding -= HandleConversationEnding;
        DialogueManager.Instance.OnChoicesPresented -= HandleChoicesPresented;
        DialogueManager.Instance.OnChoiceSelectionChanged -= HandleChoiceSelectionChanged;
        DialogueManager.Instance.OnChoiceSelected -= HandleChoiceSelected;
    }

    private void HandleLineStarted(DialogueEntry entry, string localizedText)
    {
        _currentSpeaker = entry.speaker;

        // Atenuar al 50% a los demás hablantes que tengan cartel bajado
        foreach (var kvp in _activeSigns)
        {
            if (kvp.Key != entry.speaker)
            {
                kvp.Value.SetTalkingState(false);
            }
        }

        // Si este hablante ya tiene su cartel bajado, reutilizarlo sin moverlo
        if (_activeSigns.TryGetValue(entry.speaker, out ConversationSignView existingSign))
        {
            existingSign.ShowNewLine(localizedText);
            return;
        }

        // Primera vez que habla: instanciar anclado a su posición en pantalla y bajarlo
        ConversationSignView newSign = Instantiate(signPrefab, signsContainer);
        PositionSignOnSpeaker(newSign.GetComponent<RectTransform>(), entry.speaker);
        _activeSigns[entry.speaker] = newSign;
        newSign.DropAndShow(localizedText);
    }

    private void HandleCompleteInstant()
    {
        if (_activeSigns.TryGetValue(_currentSpeaker, out ConversationSignView activeSign))
        {
            activeSign.CompleteInstant();
        }
    }

    private void HandleSkipProgress(float progress)
    {
        if (skipFillImage != null) skipFillImage.fillAmount = progress;
    }

    private void HandleConversationEnding()
    {
        if (_activeSigns.Count == 0)
        {
            DialogueManager.Instance?.NotifySignsRisen();
            return;
        }

        int remaining = _activeSigns.Count;
        foreach (var sign in _activeSigns.Values)
        {
            sign.RiseAndDestroy(() =>
            {
                remaining--;
                if (remaining <= 0)
                {
                    _activeSigns.Clear();
                    DialogueManager.Instance?.NotifySignsRisen();
                }
            });
        }
    }

    private void PositionSignOnSpeaker(RectTransform signRect, Speaker speaker)
    {
        if (!_anchorLookup.TryGetValue(speaker, out Transform anchor) || anchor == null) return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, anchor.position);
        Camera uiCam = signsContainer.GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : mainCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(signsContainer, screenPoint, uiCam, out Vector2 localPoint))
        {
            signRect.anchoredPosition = localPoint;
        }
    }

    private void HandleChoicesPresented(List<DialogueEntry> choices)
    {
        _currentSpeaker = Speaker.Sarah;

        foreach (var kvp in _activeSigns)
            if (kvp.Key != Speaker.Sarah) kvp.Value.SetTalkingState(false);

        List<string> localizedOptions = new List<string>();

        foreach (var choice in choices)
            localizedOptions.Add(GameManager.Instance.DialogueDatabase.GetText(choice.id, GameManager.Instance.CurrentLanguage));

        if (_activeSigns.TryGetValue(Speaker.Sarah, out ConversationSignView sarahSign)) {
            sarahSign.ShowChoices(localizedOptions, needsDrop: false);
        }
        else
        {
            ConversationSignView newSign = Instantiate(signPrefab, signsContainer);
            PositionSignOnSpeaker(newSign.GetComponent<RectTransform>(), Speaker.Sarah);
            _activeSigns[Speaker.Sarah] = newSign;
            newSign.ShowChoices(localizedOptions, needsDrop: true);
        }
    }

    private void HandleChoiceSelectionChanged(int index)
    {
        if (_activeSigns.TryGetValue(Speaker.Sarah, out ConversationSignView sarahSign))
            sarahSign.UpdateChoiceSelection(index);
    }

    private void HandleChoiceSelected(string choiceId)
    {
        if (_activeSigns.TryGetValue(Speaker.Sarah, out ConversationSignView sarahSign))
        {
            string text = GameManager.Instance.DialogueDatabase.GetText(choiceId, GameManager.Instance.CurrentLanguage);
            sarahSign.FreezeChosenText(text);
        }
    }
}
