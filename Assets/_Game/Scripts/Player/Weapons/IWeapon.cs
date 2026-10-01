/// <summary>
/// Interface for player weapons, defining the required methods and properties for weapon behavior.
/// </summary>
public interface IWeapon
{
    string Name { get; }

    float BaseDamage { get; set; }
    float BaseCritChance { get; set; }

    float SpecialTimer { get; }
    bool CanUseSpecial { get; }
    float AttackAnimationSpeed { get; }
    float SpecialAnimationSpeed { get; }

    bool IsComboWindowOpen { get; }
    float LastAttackTime { get; }
    int CurrentComboStep { get; }
    bool HasActiveHitbox { get; }

    bool CanAttack { get; }
    float AttackCooldownRemaining { get; }

    void TickTimers();
    void TickFallback(float failsafeSeconds);
    void ResetCombo();

    void Initialize(Player player);
    void OnEquip();
    void OnUnequip();

    void OnAttackInput();
    void OnHeavyAttackInput();

    void OnAnimationEvent(string evt);

    void OnDamageReceived();

    bool ShouldResetCombo();
    float GetDamage();
}
