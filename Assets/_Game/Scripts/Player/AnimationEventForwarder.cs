using UnityEngine;

public class AnimationEventForwarder : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    public void EnableHitbox() => player?.OnAnimationEvent("EnableHitbox");
    public void EnableHitbox12() => player?.OnAnimationEvent("EnableHitbox12");
    public void EnableHitbox3() => player?.OnAnimationEvent("EnableHitbox3");
    public void DisableHitbox() => player?.OnAnimationEvent("DisableHitbox");
    public void ExecuteSpecialAttackDamage() => player?.OnAnimationEvent("ExecuteHeavyDamage");
    public void OpenComboWindow() => player?.OnAnimationEvent("OpenComboWindow");
    public void CloseComboWindow() => player?.OnAnimationEvent("CloseComboWindow");
    public void ReturnToIdle() => player?.OnAnimationEvent("ReturnToIdle");
}
