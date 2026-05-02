using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    [Header("General Settings")] 
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioSource sfxSource;

    [Header("Ambiance Settings")] 
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip ambiance;

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

    private void PlayAmbiance()
    {
        musicSource.clip = ambiance;
        musicSource.loop = true;
        musicSource.Play();
    }

    /// <summary>
    /// Plays a sound effect once,
    /// useful for sound effects that are only played once like opening a door
    /// </summary>
    /// <param name="clip">Sound effect which will be played</param>
    /// <param name="source">AudioSource where the sound will be played from</param>
    public void PlaySound(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays a sound effect with a random pitch,
    /// useful for sound effects that are played repeatedly like footsteps
    /// </summary>
    /// <param name="clip">Sound effect which will be played</param>
    /// <param name="source">AudioSource where the sound will be played from</param>
    public void PlaySoundWithRandomPitch(AudioClip clip)
    {
        sfxSource.pitch = Random.Range(0.75f, 1.25f);
        sfxSource.PlayOneShot(clip);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "DemoScene")
        {
            PlayAmbiance();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}