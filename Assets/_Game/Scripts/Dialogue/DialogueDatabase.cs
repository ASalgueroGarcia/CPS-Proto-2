using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage
{
    EN,
    ES
}

public enum DialogueLineType
{
    Bark,
    Conversation,
    Choice
}

[Serializable]
public class DialogueEntry
{
    public string id;
    public DialogueLineType type;
    public Speaker speaker;
    [TextArea(2, 4)] public string textEN;
    [TextArea(2, 4)] public string textES;
    public string nextId;
}

[CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Dialogue/Dialogue Database")]
public class DialogueDatabase : ScriptableObject
{
    // This list will hold all dialogue entries for the game. Each entry can be accessed by its unique ID.
    [SerializeField] private List<DialogueEntry> entries = new List<DialogueEntry>();

    private Dictionary<string, DialogueEntry> _lookup;
    
    public IReadOnlyList<DialogueEntry> AllEntries => entries;

    public static readonly int NumberOfLanguages = Enum.GetValues(typeof(GameLanguage)).Length;
    public static readonly int NumberOfRequiredDataColumns = 3; // id, lineType, speaker
    public static readonly int NumberOfOptionalDataColumns = 1; // nextId
    public static readonly int NumberOfRequiredColuns = NumberOfLanguages + NumberOfRequiredDataColumns; // Required columns in the CSV file
    public static readonly int NumberOfTotalColumns = NumberOfLanguages + NumberOfRequiredDataColumns + NumberOfOptionalDataColumns; // Total columns in the CSV file

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, DialogueEntry>(entries.Count);
        foreach (var entry in entries)
        {
            if(string.IsNullOrEmpty(entry.id))
            {
                Debug.LogWarning($"[DialogueDatabase] Dialogue entry has an empty ID. This entry will be skipped: {entry}");
                continue;
            }

            if(!_lookup.TryAdd(entry.id, entry))
            {
                Debug.LogWarning($"[DialogueDatabase] Duplicate dialogue entry ID found: {entry.id}. This entry will be skipped.");
            }
        }
    }

    public void SetEntries(List<DialogueEntry> newEntries)
    {
        entries = new List<DialogueEntry>(newEntries);
        BuildLookup();
    }

    public DialogueEntry GetEntry(string id)
    {
        if (_lookup == null) BuildLookup();

        if (!_lookup.TryGetValue(id, out var entry))
        {
            Debug.LogWarning($"[DialogueDatabase] Dialogue entry with ID '{id}' not found.");
            return null;
        }

        return entry;
    }

    public string GetText(string id, GameLanguage language)
    {
        if (_lookup == null) BuildLookup();

        if (!_lookup.TryGetValue(id, out var entry))
        {
            Debug.LogWarning($"[DialogueDatabase] Dialogue entry with ID '{id}' not found.");
            return $"[MISSING: {id}]";
        }

        return language switch
        {
            GameLanguage.EN => string.IsNullOrEmpty(entry.textEN) ? $"[MISSING: {id}]" : entry.textEN,
            GameLanguage.ES => string.IsNullOrEmpty(entry.textES) ? entry.textEN : entry.textES,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
        
    }
}
