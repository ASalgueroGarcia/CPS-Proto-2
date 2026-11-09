public class PlayerDashingState : PlayerStateBase
{
    public override void Tick()
    {
        Modules.Dash.Tick();

        if (Modules.Dash.IsDashFinished()) {
            player.SwitchState(Player.PlayerState.Idle);
        }
    }

    // Single cleanup point: runs when the dash finishes and when it is cut short.
    public override void Exit() => Modules.Dash.EndDash();
}
