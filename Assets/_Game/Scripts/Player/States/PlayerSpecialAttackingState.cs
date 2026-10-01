using UnityEngine;

public class PlayerSpecialAttackingState : PlayerStateBase
{
    private const string HeavyAttackState = "HeavyAttack";

    public override void Tick()
    {
        if (Modules.Locomotion != null)
            Modules.Locomotion.SetMoveDirection(Vector3.zero);

        // Fallback: HeavyAttack has HasExitTime -> Run at 0.727, which fires before ReturnToIdle @ 1.0.
        // Detect that case and transition out so the player can act again.
        Animator anim = player.animator;
        if (anim == null || anim.IsInTransition(0)) return;

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(HeavyAttackState))
            player.SwitchState(Player.PlayerState.Idle);
    }
}
