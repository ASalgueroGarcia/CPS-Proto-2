using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The Canvas version of IConfirmDialog: a message with Yes / No over the menu. "No" is
/// selected first, so a stray Submit press can't delete a save.
/// </summary>
public class ConfirmDialogCanvasView : MonoBehaviour, IConfirmDialog
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI message;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action<bool> _onAnswer;
    private GameObject _previousSelection;

    private void Awake()
    {
        yesButton.onClick.AddListener(() => Answer(true));
        noButton.onClick.AddListener(() => Answer(false));
        panel.SetActive(false);
    }

    public void Ask(string question, Action<bool> onAnswer)
    {
        _onAnswer = onAnswer;
        message.text = question;
        panel.SetActive(true);

        if (EventSystem.current != null)
        {
            _previousSelection = EventSystem.current.currentSelectedGameObject;
            EventSystem.current.SetSelectedGameObject(noButton.gameObject);
        }
    }

    private void Answer(bool confirmed)
    {
        panel.SetActive(false);
        if (EventSystem.current != null && _previousSelection != null && _previousSelection.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(_previousSelection);
        }

        Action<bool> callback = _onAnswer;
        _onAnswer = null;
        callback?.Invoke(confirmed);
    }
}
