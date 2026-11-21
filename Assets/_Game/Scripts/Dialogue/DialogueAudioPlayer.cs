using UnityEngine;
using System;
using System.Collections.Generic;

public class DialogueAudioPlayer : MonoBehaviour
{
    [Serializable]
    public struct SpeakerLineClip
    {
        public Speaker speaker;
        public AudioClip clip;
    }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip signDropClip;
    [SerializeField] private AudioClip signRiseClip;
    [SerializeField] private AudioClip defaultNewLineClip;
    [SerializeField] private AudioClip choiceMoveClip;
    [SerializeField] private AudioClip choiceConfirmClip;

    [Header("Optional: Music Notes per speaker")]
    [SerializeField] private List<SpeakerLineClip> speakerClips = new List<SpeakerLineClip>(); 

    private Dictionary<Speaker, AudioClip> _speakerLookup;

    private void Awake()
    {
        _speakerLookup = new Dictionary<Speaker, AudioClip>();
        
        foreach (var speakerLine in speakerClips)
        {
            if(speakerLine.clip != null)
                _speakerLookup[speakerLine.speaker] = speakerLine.clip;
        }
    }

    void OnEnable()
    {
        DialogueAudioEvents.OnBarkSignDrop += PlaySignDrop;
        DialogueAudioEvents.OnBarkSignRise += PlaySignRise;
        DialogueAudioEvents.OnNewLine += PlayNewLine;
        DialogueAudioEvents.OnChoiceMoved += PlayChoiceMove;
        DialogueAudioEvents.OnChoiceConfirmed += PlayChoiceConfirm;
    }

    void OnDisable()
    {
        DialogueAudioEvents.OnBarkSignDrop -= PlaySignDrop;
        DialogueAudioEvents.OnBarkSignRise -= PlaySignRise;
        DialogueAudioEvents.OnNewLine -= PlayNewLine;
        DialogueAudioEvents.OnChoiceMoved -= PlayChoiceMove;
        DialogueAudioEvents.OnChoiceConfirmed -= PlayChoiceConfirm;
    }

    private void PlaySignDrop() => PlayClip(signDropClip);
    private void PlaySignRise() => PlayClip(signRiseClip);
    private void PlayChoiceMove() => PlayClip(choiceMoveClip);
    private void PlayChoiceConfirm() => PlayClip(choiceConfirmClip);

    private void PlayNewLine(Speaker speaker)
    {
        if(_speakerLookup != null && _speakerLookup.TryGetValue(speaker, out AudioClip clip)) {
            PlayClip(clip);          
            return;
        }
        
        PlayClip(defaultNewLineClip);
    }


    private void PlayClip(AudioClip clip)
    {
        if(clip == null || SoundManager.Instance == null) return;
        SoundManager.Instance.PlaySound(clip);
    }
     
}

