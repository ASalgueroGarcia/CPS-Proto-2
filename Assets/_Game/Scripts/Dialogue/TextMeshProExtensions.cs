using System.Globalization;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;

public static class TextMeshProExtensions
{
    /// <summary>
    /// Reveals a TextMeshPro text character by character, the TMP counterpart of DOTween's <c>DOText</c>.
    /// DOTween's own TextMeshPro module is a Pro-only asset that this project does not ship, so this
    /// mirrors the free <c>DOText</c> shortcut: same StringPlugin, same rich text and scramble options.
    /// </summary>
    /// <param name="target">The TMP text to tween</param>
    /// <param name="endValue">The final string to reveal</param>
    /// <param name="duration">The duration of the tween</param>
    /// <param name="richTextEnabled">If TRUE (default), rich text tags are interpreted while animating</param>
    /// <param name="scrambleMode">The type of scramble mode to use, if any</param>
    /// <param name="scrambleChars">A string containing the characters to use for scrambling.
    /// Leave it to NULL (default) to use the default ones</param>
    public static TweenerCore<string, string, StringOptions> DOText(
        this TMP_Text target, string endValue, float duration,
        bool richTextEnabled = true, ScrambleMode scrambleMode = ScrambleMode.None, string scrambleChars = null)
    {
        if (endValue == null)
        {
            Debug.LogWarning("You can't pass a NULL string to DOText: an empty string will be used instead to avoid errors");
            endValue = "";
        }

        TweenerCore<string, string, StringOptions> t = DOTween.To(() => target.text, x => target.text = x, endValue, duration);
        t.SetOptions(richTextEnabled, scrambleMode, scrambleChars)
            .SetTarget(target);
        return t;
    }

    /// <summary>
    /// Tweens a TextMeshPro text's counter value, with options for thousands separators.
    /// </summary>
    /// <param name="fromValue">The value to start from</param>
    /// <param name="endValue">The end value to reach</param>
    /// <param name="duration">The duration of the tween</param>
    /// <param name="addThousandsSeparator">If TRUE (default) also adds thousands separators</param>
    /// <param name="culture">The <see cref="CultureInfo"/> to use (InvariantCulture if NULL)</param>
    public static TweenerCore<int, int, NoOptions> DOCounter(
        this TMP_Text target, int fromValue, int endValue, float duration,
        bool addThousandsSeparator = true, CultureInfo culture = null)
    {
        int v = fromValue;
        CultureInfo cInfo = !addThousandsSeparator ? null : culture ?? CultureInfo.InvariantCulture;
        TweenerCore<int, int, NoOptions> t = DOTween.To(() => v, x =>
        {
            v = x;
            target.text = addThousandsSeparator
                ? v.ToString("N0", cInfo)
                : v.ToString();
        }, endValue, duration);
        t.SetTarget(target);
        return t;
    }

    /// <summary>
    /// Tweens a TextMeshPro text's color to the given value.
    /// </summary>
    /// <param name="endValue">The end value to reach</param>
    /// <param name="duration">The duration of the tween</param>
    public static TweenerCore<Color, Color, ColorOptions> DOColor(this TMP_Text target, Color endValue, float duration)
    {
        TweenerCore<Color, Color, ColorOptions> t = DOTween.To(() => target.color, x => target.color = x, endValue, duration);
        t.SetTarget(target);
        return t;
    }

    /// <summary>
    /// Tweens a TextMeshPro text's alpha color to the given value.
    /// </summary>
    /// <param name="endValue">The end value to reach</param>
    /// <param name="duration">The duration of the tween</param>
    public static TweenerCore<Color, Color, ColorOptions> DOFade(this TMP_Text target, float endValue, float duration)
    {
        TweenerCore<Color, Color, ColorOptions> t = DOTween.ToAlpha(() => target.color, x => target.color = x, endValue, duration);
        t.SetTarget(target);
        return t;
    }

    /// <summary>
    /// Displays a text with a fade-in and fade-out effect.
    /// </summary>
    /// <param name="canvasText">The TMP text that shows the dialogue</param>
    /// <param name="text">The dialogue line to reveal</param>
    /// <param name="duration">How long the reveal takes</param>
    /// <param name="holdDuration">How long the finished line stays on screen before fading out</param>
    public static void DisplayTextAndFadeOut(this TMP_Text canvasText, string text, float duration = 2f, float holdDuration = -1)
    {
        canvasText.text = "";
        canvasText.DOText(text, duration).SetEase(Ease.Linear);

        if (holdDuration > 0)
        {
            canvasText.DOFade(0, holdDuration).SetDelay(duration).SetEase(Ease.InOutQuad);
        }
    }
}
