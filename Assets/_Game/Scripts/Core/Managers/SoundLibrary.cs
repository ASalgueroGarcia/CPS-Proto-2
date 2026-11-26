using System;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library", order = 1)]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
        public bool randomizePitch = false;
        [Range(0f, 1f)] public float pitchVariation = 0.1f;
        [HideInInspector] public AudioSource source;
    }

    [SerializeField] private Sound[] sounds;

    public Sound GetSound(string soundName)
    {
        if (sounds == null) return null;

        foreach (var sound in sounds)
        {
            if (sound != null && sound.name == soundName)
                return sound;
        }

        return null;
    }

    public bool HasSound(string soundName)
    {
        return GetSound(soundName) != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sounds == null) return;

        var nameSet = new System.Collections.Generic.HashSet<string>();

        foreach (var sound in sounds)
        {
            if (sound == null) continue;

            if (string.IsNullOrWhiteSpace(sound.name) && sound.clip != null)
            {
                sound.name = Path.GetFileNameWithoutExtension(sound.clip.name);
            }

            if (string.IsNullOrWhiteSpace(sound.name))
                continue;

            if (nameSet.Contains(sound.name))
            {
                Debug.LogWarning($"[SoundLibrary] Nombre de sonido duplicado: '{sound.name}' en {name}", this);
            }
            else
            {
                nameSet.Add(sound.name);
            }
        }
    }
#endif

    public Sound[] Sounds => sounds;
}
