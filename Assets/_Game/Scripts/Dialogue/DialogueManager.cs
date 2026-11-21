using System;
using System.Collections;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public enum ConversationState
    {
        Inactive,
        Dropping, // Sign dropping (input disabled)
        Printing, // Printing text
        Waiting, // Waiting for player input
        Rising, // Sign rising (input disabled)
    }

    [Tooltip("How long the player must hold the skip input to skip the conversation.")]
    [SerializeField] private float skipHoldDuration = 1.5f;

    [Header("Animation Settings")]
    [Tooltip("If true, the DialogueManager will wait for animation callbacks to transition between states. If false, it will use a fallback duration.")]
    [SerializeField] private bool waitForAnimationCallbacks = true;
    [Tooltip("Fallback duration for sign drop/rise animations if waitForAnimationCallbacks is false.")]
    [SerializeField] private float fallbackAnimDuration = 0.5f;

    public event Action<DialogueEntry, string> OnLineStarted;
    public event Action OnCompleteLineInstantly; // Triggered when the player presses the input to complete the line instantly
    public event Action<float> OnSkipProgressChanged; // 0.0 to 1.0 for the progress of skipping
    public event Action OnConvesationEnding; // Triggered when the conversation is ending (sign rising)
    public event Action OnConversationEnded; // Triggered when the conversation has ended (sign risen)

    public ConversationState CurrentState { get; private set; } = ConversationState.Inactive;

    private DialogueEntry _currentEntry;
    private Speaker _lastSpeaker;
    private bool _hasLastSpeaker;
    private bool _isHoldingSkip;
    private float _skipTimer;
    private bool _hasActiveChoices;
    private Coroutine _transitionCoroutine;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Update()
    {
        if(!IsConversationActive) return;
        if(GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Paused) return;

        UpdateHoldSkip();
    }

    public void StartConversation(string startLineID)
    {
        if(IsConversationActive) return;
        if(GameManager.Instance != null && !GameManager.Instance.CanTriggerDialogue) return;

        DialogueEntry firstEntry = GameManager.Instance.DialogueDatabase.GetEntry(startLineID);
        if(firstEntry == null) return;

        GameManager.Instance.SetGameState(GameState.Dialogue);

        _hasLastSpeaker = false;
        _isHoldingSkip = false;
        _skipTimer = 0f;
        _hasActiveChoices = false;

        LoadLine(firstEntry);
    }

    private void LoadLine(DialogueEntry entry)
    {
        _currentEntry = entry;
        _hasActiveChoices = false;
        GameManager.Instance.DialogueSeenTracker.MarkAsSeen(entry.id);
        
        string localizedText = GameManager.Instance.DialogueDatabase.GetText(entry.id, GameManager.Instance.CurrentLanguage);

        bool needsDrop = !_hasLastSpeaker || _lastSpeaker != entry.speaker;
        _lastSpeaker = entry.speaker;
        _hasLastSpeaker = true;

        OnLineStarted?.Invoke(entry, localizedText);

        if(needsDrop)
            EnterDroppingState();
        else
            EnterPrintingState();
    }

    private void EnterDroppingState()
    {
        CurrentState = ConversationState.Dropping;
        DialogueAudioEvents.TriggerBarkSignDrop();

        if (!waitForAnimationCallbacks)
            RestartTransitionTimer(fallbackAnimDuration, NotifySignLanded);
    }

    private void EnterPrintingState()
    {
        CurrentState = ConversationState.Printing;
        DialogueAudioEvents.TriggerNewLine(_currentEntry.speaker);
    }

    private void AdvanceToNextLine()
    {
        if(string.IsNullOrEmpty(_currentEntry.nextId))
        {
            BeginEndConversation();
            return;
        }
        
        DialogueEntry nextEntry = GameManager.Instance.DialogueDatabase.GetEntry(_currentEntry.nextId);
        if(nextEntry == null)
        {
            BeginEndConversation();
            return;
        }

        LoadLine(nextEntry);
    }

    private void BeginEndConversation()
    {
        if(CurrentState == ConversationState.Rising || CurrentState == ConversationState.Inactive) return;

        CurrentState = ConversationState.Rising;
        _isHoldingSkip = false;
        _skipTimer = 0f;
        OnSkipProgressChanged?.Invoke(0f);

        DialogueAudioEvents.TriggerBarkSignRise();
        OnConvesationEnding?.Invoke();

        if (!waitForAnimationCallbacks)
            RestartTransitionTimer(fallbackAnimDuration, NotifySignsRisen);
    }

    public void NotifyTextFinishedPrinting()
    {
        if(CurrentState != ConversationState.Printing) return;
        CurrentState = ConversationState.Waiting;
    }

    public void NotifySignLanded()
    {
        if(CurrentState != ConversationState.Dropping) return;
        EnterPrintingState();
    }

    public void NotifySignsRisen()
    {
        if(CurrentState != ConversationState.Rising) return;

        CurrentState = ConversationState.Inactive;
        _currentEntry = null;

        if(GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameState.Gameplay);
        
        OnConversationEnded?.Invoke();
    }
    
    public void OnAdvanceInput()
    {
        if(!IsConversationActive) return;
        if(GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Paused) return;
        if(_hasActiveChoices) return;

        switch (CurrentState)
        {
            case ConversationState.Dropping:
            case ConversationState.Rising:
                // Ignore input while the sign is dropping or rising
                break;
            case ConversationState.Printing:
                // Complete the line instantly
                CurrentState = ConversationState.Waiting; // Move to waiting state after completing the line
                OnCompleteLineInstantly?.Invoke();
                break;
            case ConversationState.Waiting:
                AdvanceToNextLine();
                break;
        }

    }

    public void OnSkipInputStarted()
    {
        if(!IsConversationActive || _hasActiveChoices) return;
        if(CurrentState == ConversationState.Rising) return;
        _isHoldingSkip = true;
    }

    public void OnSkipInputCanceled()
    {
        _isHoldingSkip = false;
        _skipTimer = 0f;
        OnSkipProgressChanged?.Invoke(0f);
    }

    private void UpdateHoldSkip()
    {
        if(!_isHoldingSkip  || _hasActiveChoices || CurrentState == ConversationState.Rising)
        {
            if(_skipTimer > 0f)
            {
                _skipTimer = 0f;
                OnSkipProgressChanged?.Invoke(0f);
            }
            return;
        }

        _skipTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(_skipTimer / skipHoldDuration); 
        OnSkipProgressChanged?.Invoke(progress);

        if(progress >= skipHoldDuration)
            ExecuteSkip();
    }

    private void ExecuteSkip()
    {
        if(_currentEntry != null && GameManager.Instance != null)
            GameManager.Instance.DialogueSeenTracker.MarkRemainingChainAsSeen(_currentEntry.id, GameManager.Instance.DialogueDatabase);
    
        BeginEndConversation();
    }

    public void SetChoicesActive(bool isActive)
    {
        _hasActiveChoices = isActive;
        if(isActive) OnSkipInputCanceled();
    }

    public void SelectChoice(string choiceId)
    {
        if(!IsConversationActive || !_hasActiveChoices) return;

        _hasActiveChoices = false;
        DialogueAudioEvents.TriggerChoiceConfirmed();

        if(string.IsNullOrEmpty(choiceId))
        {
            BeginEndConversation();
            return;
        }

        DialogueEntry choiceEntry = GameManager.Instance.DialogueDatabase.GetEntry(choiceId);

        if(choiceEntry == null)
        {
            BeginEndConversation();
            return;
        }

        LoadLine(choiceEntry);
    }

    private void RestartTransitionTimer(float delay, Action onComplete)
    {
        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionDelayRoutine(delay, onComplete));
    }

    private IEnumerator TransitionDelayRoutine(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }


    public bool IsConversationActive => CurrentState != ConversationState.Inactive;
}
