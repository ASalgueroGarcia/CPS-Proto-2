using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    [Header("General Settings")] 
    [SerializeField] private AudioMixer mixer;

    [Header("SFX Settings")] 
    [SerializeField] private float pitchMin;
    [SerializeField] private float pitchMax;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Settings")] 
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip music;

    public static SoundManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets the track that plays in the background,
    /// can be used for ambiance or background music.
    /// </summary>
    /// <param name="bgMusic">AudioClip</param>
    public void SetBgMusic(AudioClip bgMusic)
    {
        music = bgMusic;
        musicSource.clip = music;
    }
    
    private void PlayMusic()
    {
        musicSource.loop = true;
        musicSource.Play();
    }

    /// <summary>
    /// Plays a sound effect once,
    /// useful for sound effects that are only played once like opening a door
    /// </summary>
    /// <param name="clip">Sound effect which will be played</param>
    public void PlaySound(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays a sound effect with a random pitch,
    /// useful for sound effects that are played repeatedly like footsteps
    /// </summary>
    /// <param name="clip">Sound effect which will be played</param>
    public void PlaySoundWithRandomPitch(AudioClip clip)
    {
        float pitch = Random.Range(pitchMin, pitchMax);
        Debug.Log($"PLAYING: {clip.name} | pitch: {pitch} | sfxSource active: {sfxSource.gameObject.activeInHierarchy}");
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays a random sound from an array of clips.
    /// </summary>
    /// <param name="audioClips">Array of AudioClips</param>
    public void PlayRandomSound(List<AudioClip> audioClips)
    {
        //Debug.Log($"PlayRandomSound called. Clip count: {audioClips?.Count ?? -1}");
        if (audioClips == null || audioClips.Count == 0)
        {
            Debug.LogWarning("PlayRandomSound: empty or null list.");
            return;
        }
        var soundIndex = Random.Range(0, audioClips.Count);
        PlaySoundWithRandomPitch(audioClips[soundIndex]);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}