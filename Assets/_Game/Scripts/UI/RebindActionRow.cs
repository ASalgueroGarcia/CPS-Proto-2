using System;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Inspector-serializable reference group for a single per-action controller rebind row.
/// </summary>
[Serializable]
public class RebindActionRow
{
    public string actionName;
    public string displayLabel;
    public Button rebindButton;
    public TMP_Text bindingLabel;
}
