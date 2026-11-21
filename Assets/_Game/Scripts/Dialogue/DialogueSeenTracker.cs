using System.Collections.Generic;
using UnityEngine;

public class DialogueSeenTracker
{
    private readonly HashSet<string> _seenIDs = new HashSet<string>();

    public void MarkAsSeen(string id)
    {
        if(string.IsNullOrEmpty(id)) {
            Debug.LogWarning("Dialogue ID is null or empty. Cannot mark as seen.");
            return;
        }

        _seenIDs.Add(id);
    }

    public bool HasSeen(string id)
    {
        if(string.IsNullOrEmpty(id)) {
            Debug.LogWarning("Dialogue ID is null or empty. Cannot check if seen.");
            return false;
        }

        return _seenIDs.Contains(id);
    }

    public void MarkRemainingChainAsSeen(string currentID, DialogueDatabase dialogueDatabase)
    {
        if(string.IsNullOrEmpty(currentID)) {
            Debug.LogWarning("Current dialogue ID is null or empty. Cannot mark remaining chain as seen.");
            return;
        }

        if(dialogueDatabase == null) {
            Debug.LogWarning("Dialogue database is null. Cannot mark remaining chain as seen.");
            return;
        }

        HashSet<string> visitedInThisLoop = new HashSet<string>(); // To prevent infinite loops in case of circular references
        string pointerID = currentID;

        while (!string.IsNullOrEmpty(pointerID) && visitedInThisLoop.Add(pointerID))
        {
            _seenIDs.Add(pointerID);

            DialogueEntry entry = dialogueDatabase.GetEntry(pointerID);
            
            if (entry == null) {
                Debug.LogWarning($"Dialogue entry with ID '{pointerID}' not found in the database. Stopping the chain.");
                break;
            }

            pointerID = entry.nextId;
        }
    }

    public List<string> ExportSeenIDs()
    {
        return new List<string>(_seenIDs);
    }

    public void ImportSeenIDs(IEnumerable<string> saveIDs)
    {
        ClearAll();
        if(saveIDs == null) return;

        foreach(string id in saveIDs)
        {
            if(!string.IsNullOrEmpty(id))
            {
                _seenIDs.Add(id);
            }
        }

    }

    public void ClearAll()
    {
        _seenIDs.Clear();
    }
}
