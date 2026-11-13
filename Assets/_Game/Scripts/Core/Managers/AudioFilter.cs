using UnityEngine;

public static class AudioFilter
{
    public const float OpenLowpassHz = 22000f;
    public const float OpenHighpassHz = 10f;

    /// <summary>
    /// Cutoff between open (amount 0) and filtered (amount 1). It moves in equal pitch steps,
    /// which is how the ear hears a filter sweep.
    /// </summary>
    public static float Cutoff(float openHz, float filteredHz, float amount)
    {
        return openHz * Mathf.Pow(filteredHz / openHz, Mathf.Clamp01(amount));
    }
}