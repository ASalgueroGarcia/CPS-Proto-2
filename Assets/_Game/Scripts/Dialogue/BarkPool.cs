using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New BarkPool", menuName = "Dialogue/Bark Pool")]
public class BarkPool : ScriptableObject
{
    [Tooltip("Exact IDs of the dialogue entries that will be used as barks.")]
    public List<string> linesIDs = new List<string>();
}
