using UnityEngine;

public class PlayerMovingState : PlayerStateBase
{
    public override void Tick()
    {
        if (Player.IsPaused || player.InputLocked) return;

        if (player.CheckDashInput()) return;
        if (player.CheckAttackInput()) return;
        if (player.CheckHeavyAttackInput()) return;

        Vector2 input = player.moveAction.action.ReadValue<Vector2>();
        Modules.Locomotion.UpdateFromInput(input);

        if (input == Vector2.zero)
            player.SwitchState(Player.PlayerState.Idle);
    }
}
