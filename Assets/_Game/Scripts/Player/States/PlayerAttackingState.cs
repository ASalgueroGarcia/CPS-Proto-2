public class PlayerAttackingState : PlayerStateBase
{
    private const string Attack01State = "Attack_01";
    private const string Attack02State = "Attack_02";
    private const string Attack03State = "Attack_03";

    public override void Tick()
    {
        if (Player.IsPaused) return;

        if (player.CheckHeavyAttackInput()) return;

        // Handle attack input for combo windows
        if (Modules.Weapon.CurrentWeapon != null
            && Modules.Weapon.CurrentWeapon.IsComboWindowOpen
            && Modules.Weapon.CurrentWeapon.CanAttack)
        {
            if (player.attackAction.action.WasPressedThisFrame())
                Modules.Weapon.CurrentWeapon.OnAttackInput();
        }

        // Fallback: the Animator Controller has HasExitTime transitions (e.g. Attack_01 -> Run at 0.5,
        // Attack_03 -> Run at 0.4) that fire before the ReturnToIdle animation event at 1.0. When that
        // happens the animator leaves the Attack_X state but the player state machine stays in Attacking,
        // so the player cannot move while the walking animation plays. Poll the animator to recover.
        UnityEngine.Animator anim = player.animator;
        if (anim == null || anim.IsInTransition(0)) return;

        UnityEngine.AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        bool stillInAttackClip = info.IsName(Attack01State) ||
                                 info.IsName(Attack02State) ||
                                 info.IsName(Attack03State);
        if (!stillInAttackClip)
            player.SwitchState(Player.PlayerState.Idle);
    }
}
