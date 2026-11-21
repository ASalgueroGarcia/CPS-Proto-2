using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;

public class DialogueSpeaker : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private DialogueDatabase database;
    [SerializeField] private BarkPool barkPool;

    [Header("Settings")]
    [SerializeField] private Transform dialoguePosition;
    [SerializeField] private BarkSignView barkSignViewPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private float playerDistanceThreshold = 5f;
    [SerializeField] private float barkHoldDuration = 4f;
    [SerializeField] private bool triggerOnProximity = true;
    
    private ShuffleBag<string> _idBag = new ShuffleBag<string>();
    private BarkSignView _currentBarkInstance;
    private bool _isRising = false;
    private bool _hasTrigeredThisVisit = false;
    private GameLanguage currentLanguage => GameManager.Instance.CurrentLanguage;



    private void Start()
    {
        if(barkPool == null || barkPool.linesIDs.Count == 0) return;

        RefillBag(GameManager.Instance.DialogueSeenTracker);

        GameManager.Instance.OnRoomExited += InterruptBark;
        GameManager.Instance.OnCombatStarted += InterruptBark;
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

    private void TriggerBark()
    {
        if(_currentBarkInstance != null || database == null || barkPool.linesIDs.Count == 0 || !GameManager.Instance.CanTriggerDialogue) return;
        
        var seenTracker = GameManager.Instance.DialogueSeenTracker;

        if(_idBag.Count == 0)
            RefillBag(seenTracker);

        _hasTrigeredThisVisit = true;
        _isRising = false;

        string nextId = _idBag.GetNext();
        seenTracker.MarkAsSeen(nextId); 
        string lineText = database.GetText(nextId, currentLanguage);

        _currentBarkInstance = Instantiate(barkSignViewPrefab, dialoguePosition.position, Quaternion.identity);
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

    private void RefillBag(DialogueSeenTracker seenTracker)
    {
        _idBag.Clear();
        _idBag = new ShuffleBag<string>();

        foreach (string id in barkPool.linesIDs)
        {
            if(!seenTracker.HasSeen(id))
                _idBag.Add(id);
        }
 
        if(_idBag.Count == 0)
        {
            foreach (string id in barkPool.linesIDs)
            {
                _idBag.Add(id);
            }
        }
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
}

