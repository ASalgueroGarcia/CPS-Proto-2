using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;

public class BarkSignView : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float dropOffset = 2f;
    [SerializeField] private float animDuration = 0.5f;
    [SerializeField] private float charsPerSecond = 30f;

    private Coroutine _autoRiseCoroutine;

    public void PlayBark(string text, float holdDuration, System.Action onFinished)
    {
        transform.DOKill(); // Stop any ongoing animations
        dialogueText.DOKill(); 
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;

        // Bark sign drop animation
        transform.DOMove(transform.position + Vector3.up * dropOffset, animDuration)
            .From()
            .SetEase(Ease.OutBounce)
            .OnComplete(() =>
            {
                // Start revealing the text character by character
                float printDuration = text.Length / charsPerSecond;
                DOTween.To(() => dialogueText.maxVisibleCharacters, x => dialogueText.maxVisibleCharacters = x, text.Length, printDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        // After the text is fully revealed, wait for holdDuration and then rise
                        _autoRiseCoroutine = StartCoroutine(WaitAndRise(holdDuration, onFinished));
                    });
            });
    }

    private IEnumerator WaitAndRise(float delay, System.Action onFinished)
    {
        yield return new WaitForSeconds(delay);
        Rise(false, onFinished);
    }

    public void Rise(bool immediate, System.Action onFinished)
    {
        if (_autoRiseCoroutine != null) StopCoroutine(_autoRiseCoroutine);
        
        transform.DOKill();
        dialogueText.DOKill();

        float duration = immediate ? 0.15f : animDuration;
        transform.DOMove(transform.position + Vector3.up * dropOffset, duration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                onFinished?.Invoke();
                Destroy(gameObject);
            });
    }
}