using UnityEngine;

public class PlayerIdleState : PlayerStateBase
{
    public override void Tick()
    {
        if (Player.IsPaused) return;

        // Check for state transitions based on player input
        if (player.CheckDashInput()) return;
        if (player.CheckAttackInput()) return;
        if (player.CheckHeavyAttackInput()) return;

        Vector2 input = player.moveAction.action.ReadValue<Vector2>();
        Modules.Locomotion.UpdateFromInput(input);

        if (input != Vector2.zero)
            player.SwitchState(Player.PlayerState.Moving);
    }
}
