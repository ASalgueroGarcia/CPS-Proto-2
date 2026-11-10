using UnityEngine;

/// <summary>Designer-tunable limits for runtime pitch and speed, and the pause noise layer.</summary>
[CreateAssetMenu(fileName = "AudioTuning", menuName = "Audio/Audio Tuning")]
public class AudioTuning : ScriptableObject
{
    [Header("Pitch (music and SFX)")]
    [Range(0.5f, 1f)] public float minPitch = 0.5f;
    [Range(1f, 2f)] public float maxPitch = 2f;

    [Header("Music speed")]
    [Tooltip("Speed and pitch stay independent while pitch / speed is between 0.5 and 2.")]
    [Range(0.25f, 1f)] public float minSpeed = 0.5f;
    [Range(1f, 4f)] public float maxSpeed = 2f;

    [Header("Pause noise")]
    [Tooltip("Looping noise layered over the music while the pause menu is open.")]
    public AudioClip pauseNoiseClip;
    [Range(0f, 1f)] public float pauseNoiseVolume = 0.25f;
    [Tooltip("Fade in and out, in real seconds.")]
    [Min(0f)] public float pauseNoiseFadeSeconds = 0.4f;
}