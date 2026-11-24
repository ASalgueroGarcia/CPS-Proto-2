using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New DialoguePool", menuName = "Dialogue/Dialogue Pool")]
public class DialoguePool : ScriptableObject
{
    [Tooltip("Exact IDs of the dialogue entries that will be used as barks.")]
    public List<string> linesIDs = new List<string>();
}
