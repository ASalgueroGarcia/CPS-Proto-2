using UnityEngine;

public class ButtonInteraction : MonoBehaviour
{
    [Header("Button Interaction Settings")] 
    [SerializeField] private AudioClip interactionSfx;

    public void PlayInteractionSfx()
    {
        // No SoundManager when a scene is played on its own (e.g. _MapScene). Throwing here would
        // also skip every onClick listener after this one, including the one that enters the node.
        if (SoundManager.Instance != null && interactionSfx != null) SoundManager.Instance.PlaySound(interactionSfx);
    }
}
