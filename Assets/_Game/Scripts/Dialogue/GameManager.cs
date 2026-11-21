using System;
using UnityEngine;

public enum GameState
{
    Gameplay,
    Combat,
    Dialogue,
    Paused,
}

public class GameManager : MonoBehaviour
{
    [Header("Global Dialogue Settings")]
    [SerializeField] private DialogueDatabase dialogueDatabase;
    [SerializeField] private GameLanguage currentLanguage = GameLanguage.EN;

    public static GameManager Instance { get; private set; }

    public GameState CurrentGameState { get; private set; } = GameState.Gameplay;

    public event Action OnRoomExited;
    public event Action OnCombatStarted;
    public event Action<GameLanguage> OnLanguageChanged;
    public event Action<GameState> OnGameStateChanged;

    private GameState _stateBeforePause;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        DialogueSeenTracker = new DialogueSeenTracker();
    }

    public void SetGameState(GameState newState)
    {
        if(CurrentGameState == newState) return;
        CurrentGameState = newState;

        OnGameStateChanged?.Invoke(newState);

        if(newState == GameState.Combat)
            OnCombatStarted?.Invoke();
    }

    public void SetPauseState(bool isPaused)
    {
        if(isPaused && CurrentGameState != GameState.Paused)
        {
            _stateBeforePause = CurrentGameState;
            SetGameState(GameState.Paused);
        }
        else if(!isPaused && CurrentGameState == GameState.Paused)
        {
            SetGameState(_stateBeforePause);
        }
    }

    public void SetLanguage(GameLanguage newLanguage)
    {
        if(currentLanguage == newLanguage) return;

        currentLanguage = newLanguage;
        OnLanguageChanged?.Invoke(newLanguage);
    }

    
    public void NotifyRoomExited()
    {
        OnRoomExited?.Invoke();
    }

    public DialogueDatabase DialogueDatabase => dialogueDatabase;
    public GameLanguage CurrentLanguage => currentLanguage;
    public DialogueSeenTracker DialogueSeenTracker { get; private set; }
    public bool CanTriggerDialogue => CurrentGameState == GameState.Gameplay;
}
