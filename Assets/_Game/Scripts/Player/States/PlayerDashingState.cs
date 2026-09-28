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
}
