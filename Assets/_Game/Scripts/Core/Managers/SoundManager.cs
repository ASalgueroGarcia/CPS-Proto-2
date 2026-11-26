using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Central public audio facade for the game. Handles direct AudioClip playback, mixer volume
/// synchronization with SettingsService, SoundLibrary named playback, and runtime mix controls
/// (music pitch, music speed, SFX pitch, pause vinyl noise and settings-menu distortion).
/// </summary>
public class SoundManager : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioManager audioManager;

    [Header("SFX Settings")]
    [SerializeField] private float pitchMin = 0.9f;
    [SerializeField] private float pitchMax = 1.1f;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip music;

    public static SoundManager Instance;

    public float MusicPitch => ResolveAudioManager(false) != null ? audioManager.MusicPitch : 1f;
    public float MusicSpeed => ResolveAudioManager(false) != null ? audioManager.MusicSpeed : 1f;
    public float SfxPitch => ResolveAudioManager(false) != null ? audioManager.SfxPitch : 1f;
    public bool IsPauseNoiseActive => ResolveAudioManager(false) != null && audioManager.IsPauseNoiseActive;
    public bool IsPauseNoiseAudible => ResolveAudioManager(false) != null && audioManager.IsPauseNoiseAudible;
    public bool IsMenuDistortionActive => ResolveAudioManager(false) != null && audioManager.IsMenuDistortionActive;
    public float CurrentLowpassHz => ResolveAudioManager(false) != null ? audioManager.CurrentLowpassHz : AudioFilter.OpenLowpassHz;
    public float CurrentHighpassHz => ResolveAudioManager(false) != null ? audioManager.CurrentHighpassHz : AudioFilter.OpenHighpassHz;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            ResolveAudioManager(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Volumes come from settings.json (Settings Menu sliders). Start, not Awake: the mixer
    // ignores SetFloat before then.
    private void Start()
    {
        if (Instance != this || Services.Settings == null) return;

        Services.Settings.AudioChanged += ApplyVolumes;
        ApplyVolumes(Services.Settings.Data);
    }

    private void ApplyVolumes(SettingsData settings)
    {
        AudioVolume.Apply(mixer, settings);
    }

    private AudioManager ResolveAudioManager(bool createIfMissing)
    {
        if (audioManager == null)
            audioManager = GetComponent<AudioManager>();

        if (audioManager == null)
            audioManager = AudioManager.Instance;

        if (audioManager == null && createIfMissing)
            audioManager = gameObject.AddComponent<AudioManager>();

        if (audioManager != null)
        {
            audioManager.SetFallbackMixer(mixer);
            audioManager.EnsureInitialized();
        }

        return audioManager;
    }

    #region Direct clip playback

    /// <summary>
    /// Sets the track that plays in the background,
    /// can be used for ambiance or background music.
    /// </summary>
    public void SetBgMusic(AudioClip bgMusic)
    {
        music = bgMusic;
        if (musicSource != null)
            musicSource.clip = music;
    }

    private void PlayMusic()
    {
        // Already playing: keep the track running across scene loads instead of restarting it.
        if (musicSource == null || musicSource.clip == null || musicSource.isPlaying) return;

        musicSource.loop = true;
        musicSource.Play();
    }

    /// <summary>
    /// Plays a sound effect once,
    /// useful for sound effects that are only played once like opening a door.
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays a sound effect with a random pitch,
    /// useful for sound effects that are played repeatedly like footsteps.
    /// </summary>
    public void PlaySoundWithRandomPitch(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>Plays a random sound from a list of clips.</summary>
    public void PlayRandomSound(List<AudioClip> audioClips)
    {
        if (audioClips == null || audioClips.Count == 0)
        {
            Debug.LogWarning("PlayRandomSound: empty or null list.");
            return;
        }

        int soundIndex = Random.Range(0, audioClips.Count);
        PlaySoundWithRandomPitch(audioClips[soundIndex]);
    }

    #endregion

    #region Runtime mix and distortion controls

    /// <summary>Changes the music's pitch without changing its speed. 1 = as recorded.</summary>
    public void SetMusicPitch(float pitch, float seconds = 0f)
    {
        ResolveAudioManager(true)?.SetMusicPitch(pitch, seconds);
    }

    /// <summary>Changes how fast the music plays without changing its pitch. 1 = as recorded.</summary>
    public void SetMusicSpeed(float speed, float seconds = 0f)
    {
        ResolveAudioManager(true)?.SetMusicSpeed(speed, seconds);
    }

    /// <summary>Changes the pitch of every sound effect. 1 = as recorded.</summary>
    public void SetSfxPitch(float pitch, float seconds = 0f)
    {
        ResolveAudioManager(true)?.SetSfxPitch(pitch, seconds);
    }

    /// <summary>Glides music pitch, music speed and SFX pitch back to 1.</summary>
    public void ResetPitchAndSpeed(float seconds = 0f)
    {
        ResolveAudioManager(true)?.ResetPitchAndSpeed(seconds);
    }

    /// <summary>Enables or fades out the muffled old-record pause filter and vinyl noise.</summary>
    public void SetPauseNoise(bool active)
    {
        ResolveAudioManager(true)?.SetPauseNoise(active);
    }

    /// <summary>Enables or fades out the settings-menu music distortion filter.</summary>
    public void SetMenuDistortion(bool active)
    {
        ResolveAudioManager(true)?.SetMenuDistortion(active);
    }

    /// <summary>Steps active audio fades manually (used by Update or deterministic tests).</summary>
    public void TickAudio(float unscaledDeltaTime)
    {
        ResolveAudioManager(false)?.Tick(unscaledDeltaTime);
    }

    #endregion

    #region Named SoundLibrary playback

    public void PlaySound(string soundName)
    {
        ResolveAudioManager(true)?.PlaySound(soundName);
    }

    public void PlayOneShotSound(string soundName)
    {
        ResolveAudioManager(true)?.PlayOneShotSound(soundName);
    }

    public void PlayLoopingSound(string soundName)
    {
        ResolveAudioManager(true)?.PlayLoopingSound(soundName);
    }

    public void PlaySoundAtPoint(string soundName, Vector3 position)
    {
        ResolveAudioManager(true)?.PlaySoundAtPoint(soundName, position);
    }

    public void StopSound(string soundName)
    {
        ResolveAudioManager(false)?.StopSound(soundName);
    }

    public void StopAllSounds()
    {
        ResolveAudioManager(false)?.StopAllSounds();
        if (sfxSource != null)
            sfxSource.Stop();
    }

    public bool IsSoundPlaying(string soundName)
    {
        AudioManager resolved = ResolveAudioManager(false);
        return resolved != null && resolved.IsSoundPlaying(soundName);
    }

    public void PlayMusic(string soundName, bool fadeIn = false, float fadeDuration = 1f)
    {
        ResolveAudioManager(true)?.PlayMusic(soundName, fadeIn, fadeDuration);
    }

    public void StopMusic(bool fadeOut = false, float fadeDuration = 1f)
    {
        ResolveAudioManager(false)?.StopMusic(fadeOut, fadeDuration);
        if (!fadeOut && musicSource != null)
            musicSource.Stop();
    }

    public bool IsMusicPlaying()
    {
        if (musicSource != null && musicSource.isPlaying)
            return true;

        AudioManager resolved = ResolveAudioManager(false);
        return resolved != null && resolved.IsMusicPlaying();
    }

    #endregion

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            if (Services.Settings != null)
                Services.Settings.AudioChanged -= ApplyVolumes;

            Instance = null;
        }
    }
}
