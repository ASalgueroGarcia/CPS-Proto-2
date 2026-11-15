using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Turns the Settings Menu's three volume sliders (Master, Music, SFX) into the audio mixer's
/// decibels. The parameter names must match the exposed parameters of MasterAudioMixer.
/// </summary>
public static class AudioVolume
{
    public const string MasterParameter = "MasterVolume";
    public const string MusicParameter = "MusicVolume";
    public const string SfxParameter = "SfxVolume";

    /// <summary>The mixer's silence floor; a slider at 0 maps here.</summary>
    public const float MinDecibels = -80f;

    /// <summary>Loudness is logarithmic: half the slider is about -6 dB, not half the volume.</summary>
    public static float ToDecibels(float linear)
    {
        float volume = SettingsData.ToVolume(linear);
        return volume <= 0.0001f ? MinDecibels : Mathf.Log10(volume) * 20f;
    }

    public static void Apply(AudioMixer mixer, SettingsData settings)
    {
        if (mixer == null || settings == null) return;

        mixer.SetFloat(MasterParameter, ToDecibels(settings.masterVolume));
        mixer.SetFloat(MusicParameter, ToDecibels(settings.musicVolume));
        mixer.SetFloat(SfxParameter, ToDecibels(settings.sfxVolume));
    }
}
