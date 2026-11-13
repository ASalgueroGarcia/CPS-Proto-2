using UnityEngine;

public static class AudioPitch
{
    // Limits of the mixer's Pitch Shifter effect.
    public const float MinShift = 0.5f;
    public const float MaxShift = 2f;

    /// <summary>
    /// Speeding music up also raises its pitch by the same factor. This is the correction the
    /// mixer's pitch shifter needs so the listener hears the wanted pitch at the wanted speed.
    /// </summary>
    public static float ShifterRatio(float pitch, float speed)
    {
        return Mathf.Clamp(pitch / Mathf.Max(speed, 0.01f), MinShift, MaxShift);
    }
}