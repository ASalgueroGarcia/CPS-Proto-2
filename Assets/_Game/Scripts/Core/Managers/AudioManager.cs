using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Runtime mix and named SoundLibrary backend coordinated behind SoundManager:
/// music pitch, music speed, SFX pitch, pause vinyl noise and settings-menu distortion.
/// Pitch, speed and filter parameters route through MasterAudioMixer, affecting all active sources.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public const string MusicSpeedParameter = "MusicSpeed";
    public const string MusicPitchShiftParameter = "MusicPitchShift";
    public const string SfxPitchParameter = "SfxPitch";
    public const string MusicLowpassParameter = "MusicLowpass";
    public const string MusicHighpassParameter = "MusicHighpass";

    [Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
        [Header("Pitch Variation")]
        public bool randomizePitch = false;
        [Range(0f, 1f)] public float pitchVariation = 0.1f;
        [HideInInspector] public AudioSource source;
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private int poolSize = 10;

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Tuning")]
    [Tooltip("Pitch and speed limits and the pause/menu distortion tuning. Edit the asset in Data/Audio.")]
    [SerializeField] private AudioTuning tuning;

    [Header("Sound Library")]
    [SerializeField] private SoundLibrary[] soundLibraries;
    [SerializeField] private Sound[] sounds;

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private Dictionary<string, Sound> soundDictionary;
    private List<AudioSource> audioSourcePool;
    private Queue<AudioSource> availableSources;

    private Sound currentMusicSound;
    private Coroutine musicFade;

    private AudioSource noiseSource;
    private AudioMixer fallbackMixer;
    private AudioTuning runtimeCreatedTuning;
    private bool initialized;
    private bool pauseNoiseActive;
    private bool menuDistortionActive;
    private float activeLowpassHz = 1800f;
    private float activeHighpassHz = 450f;

    private TweenedValue musicPitch = new TweenedValue(1f);
    private TweenedValue musicSpeed = new TweenedValue(1f);
    private TweenedValue sfxPitch = new TweenedValue(1f);
    private TweenedValue noiseVolume = new TweenedValue(0f);
    private TweenedValue filterAmount = new TweenedValue(0f);

    public float MusicPitch => musicPitch.Current;
    public float MusicSpeed => musicSpeed.Current;
    public float SfxPitch => sfxPitch.Current;
    public bool IsPauseNoiseActive => pauseNoiseActive;
    public bool IsMenuDistortionActive => menuDistortionActive;
    public bool IsPauseNoiseAudible => noiseSource != null && noiseSource.isPlaying;
    public float FilterAmount => filterAmount.Current;
    public float CurrentLowpassHz => AudioFilter.Cutoff(AudioFilter.OpenLowpassHz, activeLowpassHz, filterAmount.Current);
    public float CurrentHighpassHz => AudioFilter.Cutoff(AudioFilter.OpenHighpassHz, activeHighpassHz, filterAmount.Current);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);

            EnsureInitialized();
        }
        else if (Instance != this)
        {
            // Only this component: it can share its GameObject with SoundManager.
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (runtimeCreatedTuning != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeCreatedTuning);
            else
                DestroyImmediate(runtimeCreatedTuning);

            runtimeCreatedTuning = null;
        }
    }

    public void SetFallbackMixer(AudioMixer mixer)
    {
        if (mixer != null)
            fallbackMixer = mixer;
    }

    public void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (tuning == null)
        {
            runtimeCreatedTuning = ScriptableObject.CreateInstance<AudioTuning>();
            tuning = runtimeCreatedTuning;
        }

        activeLowpassHz = tuning.pauseLowpassHz;
        activeHighpassHz = tuning.pauseHighpassHz;

        soundDictionary = new Dictionary<string, Sound>(StringComparer.Ordinal);

        if (soundLibraries != null)
        {
            foreach (SoundLibrary library in soundLibraries)
            {
                if (library == null || library.Sounds == null)
                    continue;

                foreach (SoundLibrary.Sound libSound in library.Sounds)
                {
                    if (libSound == null || string.IsNullOrWhiteSpace(libSound.name))
                        continue;

                    var sound = new Sound
                    {
                        name = libSound.name,
                        clip = libSound.clip,
                        volume = libSound.volume,
                        pitch = libSound.pitch,
                        loop = libSound.loop,
                        randomizePitch = libSound.randomizePitch,
                        pitchVariation = libSound.pitchVariation
                    };

                    if (!soundDictionary.ContainsKey(sound.name))
                        soundDictionary.Add(sound.name, sound);
                    else
                        Debug.LogWarning($"[AudioManager] Duplicate sound '{sound.name}' - keeping first entry.");
                }
            }
        }

        if (sounds != null)
        {
            foreach (Sound s in sounds)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.name))
                    continue;

                if (!soundDictionary.ContainsKey(s.name))
                    soundDictionary.Add(s.name, s);
            }
        }

        int count = Mathf.Max(1, poolSize);
        audioSourcePool = new List<AudioSource>(count);
        availableSources = new Queue<AudioSource>(count);

        for (int i = 0; i < count; i++)
        {
            AudioSource src = NewSource(sfxGroup, false);
            audioSourcePool.Add(src);
            availableSources.Enqueue(src);
        }

        if (musicSource == null) musicSource = NewSource(musicGroup, true);
        if (sfxSource == null) sfxSource = NewSource(sfxGroup, false);

        // The noise sits on Master so music pitch and speed never colour it.
        noiseSource = NewSource(masterGroup, true);
        noiseSource.volume = 0f;
    }

    private AudioSource NewSource(AudioMixerGroup group, bool loop)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.outputAudioMixerGroup = group;
        return src;
    }

    private void Update()
    {
        Tick(Time.unscaledDeltaTime);
    }

    /// <summary>Steps active pitch, speed, filter and noise fades using unscaled time.</summary>
    public void Tick(float unscaledDeltaTime)
    {
        EnsureInitialized();

        bool pitchMoved = musicPitch.Step(unscaledDeltaTime) | musicSpeed.Step(unscaledDeltaTime) | sfxPitch.Step(unscaledDeltaTime);
        if (pitchMoved) ApplyPitchAndSpeed();
        if (noiseVolume.Step(unscaledDeltaTime)) ApplyNoise();
        if (filterAmount.Step(unscaledDeltaTime)) ApplyMuffle();
    }

    #region Pitch, speed, pause noise and menu distortion

    /// <summary>Changes the music's pitch without changing its speed. 1 = as recorded.</summary>
    /// <param name="seconds">Time to glide there, in real seconds. 0 = at once.</param>
    public void SetMusicPitch(float pitch, float seconds = 0f)
    {
        EnsureInitialized();
        musicPitch.MoveTo(Mathf.Clamp(pitch, tuning.minPitch, tuning.maxPitch), seconds);
    }

    /// <summary>Changes how fast the music plays without changing its pitch. 1 = as recorded.</summary>
    /// <param name="seconds">Time to glide there, in real seconds. 0 = at once.</param>
    public void SetMusicSpeed(float speed, float seconds = 0f)
    {
        EnsureInitialized();
        musicSpeed.MoveTo(Mathf.Clamp(speed, tuning.minSpeed, tuning.maxSpeed), seconds);
    }

    /// <summary>Changes the pitch of every sound effect (their speed changes with it). 1 = as recorded.</summary>
    /// <param name="seconds">Time to glide there, in real seconds. 0 = at once.</param>
    public void SetSfxPitch(float pitch, float seconds = 0f)
    {
        EnsureInitialized();
        sfxPitch.MoveTo(Mathf.Clamp(pitch, tuning.minPitch, tuning.maxPitch), seconds);
    }

    public void ResetPitchAndSpeed(float seconds = 0f)
    {
        EnsureInitialized();
        musicPitch.MoveTo(1f, seconds);
        musicSpeed.MoveTo(1f, seconds);
        sfxPitch.MoveTo(1f, seconds);
    }

    /// <summary>
    /// Pause sound: the music turns thin and muffled, like an old record heard from another room,
    /// with the noise layered on top. Pass false to fade back. Works while the game is paused.
    /// </summary>
    public void SetPauseNoise(bool on)
    {
        EnsureInitialized();
        pauseNoiseActive = on;
        RefreshFilterAndNoiseTargets(tuning.pauseNoiseFadeSeconds);
    }

    /// <summary>
    /// Settings-menu distortion: muffles the background music while the player adjusts options,
    /// and preserves an active pause filter if Settings was opened from the Pause Menu.
    /// </summary>
    public void SetMenuDistortion(bool active)
    {
        EnsureInitialized();
        menuDistortionActive = active;
        RefreshFilterAndNoiseTargets(tuning.menuDistortionFadeSeconds);
    }

    private void RefreshFilterAndNoiseTargets(float releaseSeconds)
    {
        if (pauseNoiseActive)
        {
            activeLowpassHz = tuning.pauseLowpassHz;
            activeHighpassHz = tuning.pauseHighpassHz;
            filterAmount.MoveTo(1f, tuning.pauseNoiseFadeSeconds);
            float targetNoise = tuning.pauseNoiseClip != null ? tuning.pauseNoiseVolume : 0f;
            noiseVolume.MoveTo(targetNoise, tuning.pauseNoiseFadeSeconds);
            ApplyMuffle();
            return;
        }

        if (menuDistortionActive)
        {
            activeLowpassHz = tuning.menuLowpassHz;
            activeHighpassHz = tuning.menuHighpassHz;
            filterAmount.MoveTo(1f, tuning.menuDistortionFadeSeconds);
            float targetNoise = tuning.pauseNoiseClip != null ? tuning.menuNoiseVolume : 0f;
            noiseVolume.MoveTo(targetNoise, tuning.menuDistortionFadeSeconds);
            ApplyMuffle();
            return;
        }

        filterAmount.MoveTo(0f, releaseSeconds);
        noiseVolume.MoveTo(0f, releaseSeconds);
    }

    private AudioMixer ResolveMixer()
    {
        if (musicGroup != null && musicGroup.audioMixer != null)
            return musicGroup.audioMixer;

        if (masterGroup != null && masterGroup.audioMixer != null)
            return masterGroup.audioMixer;

        return fallbackMixer;
    }

    private void ApplyPitchAndSpeed()
    {
        AudioMixer mixer = ResolveMixer();
        if (mixer == null) return;

        mixer.SetFloat(MusicSpeedParameter, musicSpeed.Current);
        mixer.SetFloat(MusicPitchShiftParameter, AudioPitch.ShifterRatio(musicPitch.Current, musicSpeed.Current));
        mixer.SetFloat(SfxPitchParameter, sfxPitch.Current);
    }

    private void ApplyMuffle()
    {
        AudioMixer mixer = ResolveMixer();
        if (mixer == null) return;

        mixer.SetFloat(MusicLowpassParameter, CurrentLowpassHz);
        mixer.SetFloat(MusicHighpassParameter, CurrentHighpassHz);
    }

    private void ApplyNoise()
    {
        if (noiseSource == null) return;

        noiseSource.volume = noiseVolume.Current;
        if (noiseVolume.Current > 0f && !noiseSource.isPlaying && tuning.pauseNoiseClip != null)
        {
            noiseSource.clip = tuning.pauseNoiseClip;
            noiseSource.Play();
        }
        else if (noiseVolume.Current <= 0f && noiseSource.isPlaying)
        {
            noiseSource.Stop();
        }
    }

    #endregion

    public void PlaySound(string soundName)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(soundName) || !soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogWarning("Sound " + soundName + " not found!");
            return;
        }

        if (sound.loop) PlayLoopingSound(soundName);
        else PlayOneShotSound(soundName);
    }

    public void PlayOneShotSound(string soundName)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(soundName) || !soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogWarning("Sound " + soundName + " not found!");
            return;
        }

        if (sound.clip == null) return;

        float pitch = GetPitch(sound);
        float volScale = sound.volume * sfxVolume * masterVolume;

        if (sfxSource != null)
        {
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(sound.clip, volScale);
        }
    }

    public void PlayLoopingSound(string soundName)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(soundName) || !soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogWarning("Sound " + soundName + " not found!");
            return;
        }

        if (sound.clip == null) return;

        if (sound.source != null && sound.source.isPlaying)
        {
            ApplyLoopingSoundVolume(sound);
            return;
        }

        AudioSource source = GetAvailableSource();
        if (source == null)
        {
            Debug.LogWarning("No available audio sources in pool!");
            return;
        }

        sound.source = source;

        source.clip = sound.clip;
        source.loop = true;
        source.playOnAwake = false;

        source.pitch = GetPitch(sound);
        ApplyLoopingSoundVolume(sound);

        source.Play();
    }

    public void PlaySoundAtPoint(string soundName, Vector3 position)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(soundName) || !soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogWarning("Sound " + soundName + " not found!");
            return;
        }

        if (sound.clip == null) return;

        AudioSource.PlayClipAtPoint(sound.clip, position, sound.volume * sfxVolume * masterVolume);
    }

    public void PlayMusic(string soundName, bool fadeIn = false, float fadeDuration = 1f)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(soundName) || !soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogWarning("Music " + soundName + " not found!");
            return;
        }

        if (sound.clip == null) return;

        StopMusicFade();
        if (fadeIn)
        {
            musicFade = StartCoroutine(FadeToMusic(sound, fadeDuration));
            return;
        }

        currentMusicSound = sound;

        musicSource.clip = sound.clip;
        musicSource.pitch = GetPitch(sound);
        musicSource.loop = true;
        musicSource.volume = sound.volume * musicVolume * masterVolume;
        musicSource.Play();
    }

    public void StopSound(string soundName)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(soundName) || !soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            Debug.LogWarning("Sound " + soundName + " not found!");
            return;
        }

        if (sound.source != null)
        {
            sound.source.Stop();
            ReturnSourceToPool(sound.source);
            sound.source = null;
        }
    }

    public void StopAllSounds()
    {
        EnsureInitialized();
        foreach (Sound s in soundDictionary.Values)
        {
            if (s.source != null)
            {
                s.source.Stop();
                ReturnSourceToPool(s.source);
                s.source = null;
            }
        }

        if (sfxSource != null) sfxSource.Stop();

        RebuildAvailableQueue();
    }

    public void StopMusic(bool fadeOut = false, float fadeDuration = 1f)
    {
        EnsureInitialized();
        if (!musicSource) return;

        StopMusicFade();
        if (!fadeOut)
        {
            musicSource.Stop();
            currentMusicSound = null;
            return;
        }

        musicFade = StartCoroutine(FadeOutMusic(fadeDuration));
    }

    public void SetMasterVolume(float volume)
    {
        EnsureInitialized();
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        EnsureInitialized();
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
    }

    public void SetSFXVolume(float volume)
    {
        EnsureInitialized();
        sfxVolume = Mathf.Clamp01(volume);
        UpdateSfxVolumes();
    }

    private void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        UpdateSfxVolumes();
    }

    private void UpdateMusicVolume()
    {
        if (!musicSource) return;
        if (musicSource.clip == null) return;

        float baseVol = currentMusicSound != null ? currentMusicSound.volume : 1f;
        musicSource.volume = baseVol * musicVolume * masterVolume;
    }

    private void UpdateSfxVolumes()
    {
        foreach (Sound s in soundDictionary.Values)
        {
            if (!s.loop || s.source == null || !s.source.isPlaying)
                continue;

            ApplyLoopingSoundVolume(s);
        }
    }

    private void ApplyLoopingSoundVolume(Sound sound)
    {
        if (sound == null || sound.source == null) return;
        sound.source.volume = sound.volume * sfxVolume * masterVolume;
    }

    private float GetPitch(Sound sound)
    {
        float p = sound != null ? sound.pitch : 1f;
        if (sound != null && sound.randomizePitch && sound.pitchVariation > 0f)
        {
            float v = Mathf.Clamp01(sound.pitchVariation);
            p *= UnityEngine.Random.Range(1f - v, 1f + v);
        }

        return Mathf.Clamp(p, 0.1f, 3f);
    }

    private AudioSource GetAvailableSource()
    {
        int guard = availableSources.Count;
        while (guard-- > 0 && availableSources.Count > 0)
        {
            AudioSource src = availableSources.Dequeue();
            if (src != null && !src.isPlaying)
                return src;

            if (src != null)
                availableSources.Enqueue(src);
        }

        foreach (AudioSource src in audioSourcePool)
        {
            if (src != null && !src.isPlaying)
                return src;
        }

        return null;
    }

    private void ReturnSourceToPool(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.pitch = 1f;
        source.volume = 1f;

        if (!availableSources.Contains(source))
            availableSources.Enqueue(source);
    }

    private void RebuildAvailableQueue()
    {
        availableSources.Clear();
        foreach (AudioSource src in audioSourcePool)
        {
            if (src != null && !src.isPlaying)
                availableSources.Enqueue(src);
        }
    }

    private void StopMusicFade()
    {
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = null;
    }

    // Fades run on unscaled time so they finish while the game is paused.
    private IEnumerator FadeToMusic(Sound newSound, float duration)
    {
        if (!musicSource) yield break;

        float start = musicSource.volume;
        float end = 0f;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, end, t / dur);
            yield return null;
        }

        musicSource.volume = 0f;

        currentMusicSound = newSound;
        musicSource.clip = newSound.clip;
        musicSource.pitch = GetPitch(newSound);
        musicSource.loop = true;
        musicSource.Play();

        start = 0f;
        end = newSound.volume * musicVolume * masterVolume;

        t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, end, t / dur);
            yield return null;
        }

        musicSource.volume = end;
        musicFade = null;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        if (!musicSource) yield break;

        float start = musicSource.volume;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        currentMusicSound = null;
        musicFade = null;
    }

    public bool IsSoundPlaying(string soundName)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(soundName) || !soundDictionary.TryGetValue(soundName, out Sound sound))
            return false;

        return sound.source != null && sound.source.isPlaying;
    }

    public bool IsMusicPlaying()
    {
        EnsureInitialized();
        return musicSource != null && musicSource.isPlaying;
    }

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
}
