using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;
    private CanvasGroup group;
    private Coroutine fade;

    public static ScreenFader Instance
    {
        get
        {
            if (instance == null) instance = Create();
            return instance;
        }
    }

    public void Set(float alpha)
    {
        if (fade != null) StopCoroutine(fade);
        group.alpha = alpha;
    }

    public Coroutine FadeTo(float alpha, float duration)
    {
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(Fade(alpha, duration));
        return fade;
    }

    private IEnumerator Fade(float target, float duration)
    {
        float from = group.alpha;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, target, t / duration);
            yield return null;
        }

        group.alpha = target;
        fade = null;
    }

    private static ScreenFader Create()
    {
        GameObject root = new GameObject("ScreenFader", typeof(Canvas), typeof(CanvasGroup));
        DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        GameObject black = new GameObject("Black", typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)black.transform;
        rect.SetParent(root.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image image = black.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        ScreenFader fader = root.AddComponent<ScreenFader>();
        fader.group = root.GetComponent<CanvasGroup>();
        fader.group.alpha = 0f;
        fader.group.blocksRaycasts = false;
        fader.group.interactable = false;
        return fader;
    }
}
