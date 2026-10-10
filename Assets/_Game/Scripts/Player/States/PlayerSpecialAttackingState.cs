using UnityEngine;

public class PlayerSpecialAttackingState : PlayerStateBase
{
    private const string HeavyAttackState = "HeavyAttack";

    public override void Tick()
    {
        if (Modules.Locomotion != null)
            Modules.Locomotion.SetMoveDirection(Vector3.zero);

        // Fallback: If the player is no longer in a heavy attack animation, switch to idle state
        Animator anim = player.animator;
        if (anim == null || anim.IsInTransition(0)) return;

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(HeavyAttackState))
            player.SwitchState(Player.PlayerState.Idle);
    }

    // Defense in depth: kill any residual hitbox on exit.
    public override void Exit() => Modules.Weapon.OnAnimationEvent("DisableHitbox");
}
