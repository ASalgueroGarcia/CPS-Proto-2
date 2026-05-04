using UnityEngine;

public class ButtonInteraction : MonoBehaviour
{
    [Header("Button Interaction Settings")] 
    [SerializeField] private AudioClip interactionSfx;

    public void PlayInteractionSfx()
    {
        SoundManager.Instance.PlaySound(interactionSfx);
    }
}
