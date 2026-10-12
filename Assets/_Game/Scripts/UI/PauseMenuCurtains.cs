using System.Collections;
using UnityEngine;

/// <summary>
/// Pause menu entrance (GDD > Menus: Art and main functions > Pause Menu, artistic part):
/// the curtains close, then the signs drop in on their chains.
/// Runs on unscaled time because the game is paused while this plays.
/// </summary>
public class PauseMenuCurtains : MonoBehaviour
{
    [Header("Moving Parts")]
    [SerializeField] private RectTransform leftCurtain;
    [SerializeField] private RectTransform rightCurtain;
    [SerializeField] private RectTransform signs;

    [Header("Timing (seconds, unscaled)")]
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private float dropDuration = 0.45f;

    private void OnEnable()
    {
        StartCoroutine(PlayEntrance());
    }

    private void OnDisable()
    {
        // Leave the layout in its final pose so it reads correctly in the editor and on the next open.
        SetPose(1f, 1f);
    }

    private IEnumerator PlayEntrance()
    {
        SetPose(0f, 0f);

        for (float t = 0f; t < closeDuration; t += Time.unscaledDeltaTime)
        {
            SetPose(EaseOutCubic(t / closeDuration), 0f);
            yield return null;
        }

        for (float t = 0f; t < dropDuration; t += Time.unscaledDeltaTime)
        {
            SetPose(1f, EaseOutBack(t / dropDuration));
            yield return null;
        }

        SetPose(1f, 1f);
    }

    /// <param name="close">0 = curtains open (off-screen), 1 = closed.</param>
    /// <param name="drop">0 = signs above the screen, 1 = in place.</param>
    private void SetPose(float close, float drop)
    {
        // Shifting anchors rather than positions keeps this independent of screen size.
        float open = 1f - close;
        if (leftCurtain != null) SetAnchorsX(leftCurtain, -0.5f * open, 0.5f - 0.5f * open);
        if (rightCurtain != null) SetAnchorsX(rightCurtain, 0.5f + 0.5f * open, 1f + 0.5f * open);
        if (signs != null)
        {
            float lift = 1f - drop;
            signs.anchorMin = new Vector2(0f, lift);
            signs.anchorMax = new Vector2(1f, 1f + lift);
        }
    }

    private static void SetAnchorsX(RectTransform rect, float minX, float maxX)
    {
        rect.anchorMin = new Vector2(minX, 0f);
        rect.anchorMax = new Vector2(maxX, 1f);
    }

    private static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }

    // Slight overshoot so the signs settle like they swung on their chains.
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.2f;
        const float c3 = c1 + 1f;
        float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }
}
