public class PlayerAttackingState : PlayerStateBase
{

#region Constants

    private const string Attack01State = "Attack_01";
    private const string Attack02State = "Attack_02";
    private const string Attack03State = "Attack_03";

#endregion

    public override void Tick()
    {
        if (Player.IsPaused) return;

        if (player.CheckHeavyAttackInput()) return;
        if (player.CheckAttackInput()) return;

        // Fallback: If the player is no longer in an attack animation, switch to idle state
        UnityEngine.Animator anim = player.animator;
        if (anim == null || anim.IsInTransition(0)) return;

        UnityEngine.AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        bool stillInAttackClip = info.IsName(Attack01State) ||
                                 info.IsName(Attack02State) ||
                                 info.IsName(Attack03State);
        if (!stillInAttackClip)
            player.SwitchState(Player.PlayerState.Idle);
    }

    // Ensure the hitbox is killed on any exit path (combo end, heavy attack interrupt, etc.)
    public override void Exit() => Modules.Weapon.OnAnimationEvent("DisableHitbox");
}
