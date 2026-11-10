using UnityEngine;

/// <summary>Cutoff maths for the mixer's lowpass and highpass filters.</summary>
public static class AudioFilter
{
    /// <summary>Lowpass cutoff that lets everything through.</summary>
    public const float OpenLowpassHz = 22000f;
    /// <summary>Highpass cutoff that lets everything through.</summary>
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