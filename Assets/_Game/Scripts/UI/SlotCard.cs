using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One save slot on the slot screen: the card itself plays the slot, the small button deletes it.
/// The wording comes from SlotCardText, so an in-world slot view says exactly the same.
/// </summary>
public class SlotCard : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI details;

    public Button PlayButton => playButton;

    public void Bind(int slot, Action<int> onChosen, Action<int> onDelete)
    {
        playButton.onClick.AddListener(() => onChosen(slot));
        deleteButton.onClick.AddListener(() => onDelete(slot));
    }

    public void Show(SlotSummary summary)
    {
        title.text = SlotCardText.Title(summary);
        details.text = SlotCardText.Details(summary);
        deleteButton.gameObject.SetActive(summary.Exists);
    }
}
