public class PlayerAttackingState : PlayerStateBase
{
    public override void Tick()
    {
        if (Player.IsPaused) return;

        if (player.CheckHeavyAttackInput()) return;

        // Handle attack input for combo windows
        if (Modules.Weapon.CurrentWeapon != null && Modules.Weapon.CurrentWeapon.IsComboWindowOpen)
        {
            if (player.attackAction.action.WasPressedThisFrame())
                Modules.Weapon.CurrentWeapon.OnAttackInput();
        }
    }
}
