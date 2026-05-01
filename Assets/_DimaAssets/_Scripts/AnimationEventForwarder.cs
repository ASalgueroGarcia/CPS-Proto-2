using UnityEngine;

public class AnimationEventForwarder : MonoBehaviour
{
    private PlayerFSM playerFSM;

    private void Awake()
    {
        // Find the PlayerFSM on the parent
        playerFSM = GetComponentInParent<PlayerFSM>();
    }

    public void EnableHitbox() => playerFSM?.EnableHitbox();
    public void EnableHitbox12() => playerFSM?.EnableHitbox12();
    public void EnableHitbox3() => playerFSM?.EnableHitbox3();
    public void DisableHitbox() => playerFSM?.DisableHitbox();
    public void ExecuteSpecialAttackDamage() => playerFSM?.ExecuteSpecialAttackDamage();
    
    // New methods for the Timing-Based Combo System
    public void OpenComboWindow() => playerFSM?.OpenComboWindow();
    public void CloseComboWindow() => playerFSM?.CloseComboWindow();
    public void ReturnToIdle() => playerFSM?.ReturnToIdle();
}