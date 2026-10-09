public class PlayerDashingState : PlayerStateBase
{
    public override void Tick()
    {
        Modules.Dash.Tick();

        if (Modules.Dash.IsDashFinished()) {
            Modules.Dash.EndDash();
            player.SwitchState(Player.PlayerState.Idle);
        }
    }

    // Safety net: if the state is exited before the dash naturally ends
    // (e.g. via SwitchState from another path), make sure EndDash still runs.
    public override void Exit() => Modules.Dash.EndDash();
}
