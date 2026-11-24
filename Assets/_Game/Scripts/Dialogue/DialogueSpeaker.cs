using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueSpeaker : MonoBehaviour
{
    [Header("Speaker Settings")]
    [SerializeField] private Speaker speakerType;
    [SerializeField] private DialoguePool barkPool;
    [Tooltip("IDs of the dialogue entries that will be used as barks for this speaker. Empty for NPCs with only barks")]
    [SerializeField] private DialoguePool conversationBarkPool;


    [Header("Scene and Prefab References")]
    [SerializeField] private Transform barkDialoguePosition;
    [SerializeField] private Transform conversationPosition;
    [SerializeField] private BarkSignView barkSignViewPrefab;
    [SerializeField] private float playerDistanceThreshold = 5f;
    [SerializeField] private float barkHoldDuration = 4f;
    [SerializeField] private bool triggerOnProximity = true;
    
    private ShuffleBag<string> _barkBag = new ShuffleBag<string>();
    private ShuffleBag<string> _convBag = new ShuffleBag<string>();

    private BarkSignView _currentBarkInstance;
    
    private bool _isRising = false;
    private bool _hasTrigeredThisVisit = false;
    
    private GameLanguage currentLanguage => GameManager.Instance.CurrentLanguage;
    private Transform player => GameManager.Instance.PlayerTransform;

    private void Start()
    {
        var tracker = GameManager.Instance.DialogueSeenTracker;

        if (barkPool != null && barkPool.linesIDs.Count != 0)
            RefillBag(_barkBag, barkPool.linesIDs, tracker);
            
        if(HasConversation)
            RefillBag(_convBag, conversationBarkPool.linesIDs, tracker);

        GameManager.Instance.OnRoomExited += InterruptBark;
        GameManager.Instance.OnCombatStarted += InterruptBark;
    }

    private void OnDestroy()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnRoomExited -= InterruptBark;
            GameManager.Instance.OnCombatStarted -= InterruptBark;
        }
    }

    private void Update()
    {
        if(!triggerOnProximity) return;

        bool playerInRange = IsPlayerInRange();
        
        if(playerInRange && _currentBarkInstance == null && !_hasTrigeredThisVisit)
            TriggerBark();

        if (!playerInRange)
        {
            _hasTrigeredThisVisit = false;

            if(_currentBarkInstance != null && !_isRising)
                InterruptBark();
        }

    }

    public void TriggerBark()
    {
        if (_currentBarkInstance != null || barkPool == null || barkPool.linesIDs.Count == 0) return;
        if (!GameManager.Instance.CanTriggerDialogue) return;
        
        _hasTrigeredThisVisit = true;
        _isRising = false;

        var seenTracker = GameManager.Instance.DialogueSeenTracker;

        if (_barkBag.Count == 0)
            RefillBag(_barkBag, barkPool.linesIDs, seenTracker);


        string nextId = _barkBag.GetNext();
        seenTracker.MarkAsSeen(nextId); 
        
        string lineText = GameManager.Instance.DialogueDatabase.GetText(nextId, currentLanguage);

        _currentBarkInstance = Instantiate(barkSignViewPrefab, barkDialoguePosition.position, Quaternion.identity);
        _currentBarkInstance.PlayBark(lineText, barkHoldDuration, () =>
        {
            _currentBarkInstance = null;
            _isRising = false;
        });
    }

    public void InterruptBark()
    {
        if(_currentBarkInstance == null || _isRising) return;

        _isRising = true;
        _currentBarkInstance.Rise(immediate: true, onFinished: () =>
        {
            _currentBarkInstance = null;
            _isRising = false;
        });
    }

    public void Interact()
    {
        if(!HasConversation) return;
        if(!GameManager.Instance.CanTriggerDialogue) return;

        InterruptBark();

        var seenTracker = GameManager.Instance.DialogueSeenTracker;

        if (_convBag.Count == 0)
            RefillBag(_convBag, conversationBarkPool.linesIDs, seenTracker);

        string startId = _convBag.GetNext();
        DialogueManager.Instance.StartConversation(startId);
    }

    private void RefillBag(ShuffleBag<string> bag, List<string> sourceIDs, DialogueSeenTracker seenTracker)
    {
        foreach (string id in sourceIDs)
            if(!seenTracker.HasSeen(id))
                bag.Add(id);
 
        if(bag.Count == 0)
            foreach (string id in barkPool.linesIDs)
                bag.Add(id);
    }

    private bool IsPlayerInRange()
    {
        return (player.position - transform.position).sqrMagnitude < playerDistanceThreshold * playerDistanceThreshold;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDistanceThreshold);
    }

    public Speaker SpeakerType => speakerType;
    public bool HasConversation => conversationBarkPool != null && conversationBarkPool.linesIDs.Count > 0;
}

