using UnityEngine;

/// <summary>
/// This class handles the player's weapon system, including equipping weapons, handling attack inputs, and managing weapon states.
/// It provides methods to equip weapons, process attack inputs, and interact with the current weapon's functionality.
/// </summary>
public class PlayerWeaponController : MonoBehaviour
{

#region Fields

    private Player _player;

#endregion
#region Methods

    public void Initialize(Player player)
    {
        _player = player;
        EquipDefaultWeapon();
    }

    private void EquipDefaultWeapon()
    {
        IWeapon weapon = GetComponent<IWeapon>();
        if (weapon != null)
            EquipWeapon(weapon);
    }

    public void EquipWeapon(IWeapon weapon)
    {
        CurrentWeapon?.OnUnequip();
        CurrentWeapon = weapon;
        weapon.Initialize(_player);
        CurrentWeapon?.OnEquip();
    }

    public void OnAttackInput()
    {
        if (CurrentWeapon == null) return;

        if (!CurrentWeapon.CanAttack) return;

        if (_player.currentState == Player.PlayerState.Attacking && !CurrentWeapon.IsComboWindowOpen)
            return;

        CurrentWeapon.OnAttackInput();
        _player.SwitchState(Player.PlayerState.Attacking);
    }

    public void OnHeavyAttackInput()
    {
        if (CurrentWeapon == null) return;
        if (!CurrentWeapon.CanUseSpecial) return;

        CurrentWeapon.OnHeavyAttackInput();
        _player.SwitchState(Player.PlayerState.SpecialAttacking);
    }

    public void OnAnimationEvent(string evt)
    {
        CurrentWeapon?.OnAnimationEvent(evt);
    }

    public void OnDamageReceived(float dmg)
    {
        CurrentWeapon?.OnDamageReceived();
    }

    public float GetPlayerDamage()
    {
        return CurrentWeapon?.GetDamage() ?? 0f;
    }

    public float GetPlayerLastAttackTime()
    {
        return CurrentWeapon?.LastAttackTime ?? 0f;
    }

    public bool ShouldResetCombo()
    {
        return CurrentWeapon?.ShouldResetCombo() ?? false;
    }

    // Weapon-specific timers and combo management are handled through IWeapon interface methods.
    public void TickWeapon()
    {
        CurrentWeapon?.TickTimers();
    }

    public void TickFallback(float failsafeSeconds)
    {
        CurrentWeapon?.TickFallback(failsafeSeconds);
    }

    public void ResetCombo()
    {
        CurrentWeapon?.ResetCombo();
    }

#endregion
#region Properties

    public IWeapon CurrentWeapon { get; private set; }

#endregion

}
