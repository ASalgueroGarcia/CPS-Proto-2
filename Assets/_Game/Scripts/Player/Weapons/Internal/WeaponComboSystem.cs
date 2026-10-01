using UnityEngine;

/// <summary>
/// Identifies a combo step. The weapon is responsible for mapping this to its own result data.
/// </summary>
public readonly struct ComboStep
{
    /// <summary>
    /// Index is 1-based, so step 1 is the first attack in the combo, step 2 is the second, etc.
    /// </summary>
    public readonly int Index;

    public ComboStep(int index) { Index = index; }
}


/// <summary>
/// Generic combo system. Holds combo state (step counter, combo window, fallback timer)
/// without knowing about any specific weapon. The weapon is responsible for mapping a
/// given step index to its own damage / color / animation result.
/// </summary>
public class WeaponComboSystem
{

#region Fields and Properties

    private int _step = 0;
    private bool _windowOpen = false;
    private float _lastAttackTime = 0f;
    private float _fallbackTimer = 0f;
    private readonly float _resetTime;
    private readonly float _attackCooldown;
    private readonly int _maxSteps;

    public int CurrentStep => _step;
    public bool IsWindowOpen => _windowOpen;
    public float LastAttackTime => _lastAttackTime;
    public float FallbackTimer => _fallbackTimer;
    public float TimeSinceLastStep => _step > 0 ? Time.time - _lastAttackTime : Mathf.Infinity;
    public int MaxSteps => _maxSteps;
    public float AttackCooldown => _attackCooldown;

    /// <summary>True when no combo is active or the cooldown has elapsed since the last attack.</summary>
    public bool CanAttack => _step == 0 || (Time.time - _lastAttackTime) >= _attackCooldown;

    /// <summary>Seconds remaining until the next attack is allowed. Returns 0 when ready.</summary>
    public float AttackCooldownRemaining => Mathf.Max(0f, _attackCooldown - (Time.time - _lastAttackTime));

#endregion

    public WeaponComboSystem(float resetTime, float attackCooldown, int maxSteps = 3)
    {
        _resetTime = Mathf.Max(0.01f, resetTime);
        _attackCooldown = Mathf.Max(0f, attackCooldown);
        _maxSteps = Mathf.Max(1, maxSteps);
    }

#region Public Methods

    /// <summary>
    /// Advances to the next step, wrapping at <see cref="MaxSteps"/> (e.g. 1,2,3,1,2,3,...).
    /// Caller maps the returned step index to its own result data.
    /// </summary>
    public ComboStep AdvanceStep()
    {
        _step = (_step % _maxSteps) + 1;
        _lastAttackTime = Time.time;
        _windowOpen = false;
        _fallbackTimer = 0f;
        return new ComboStep(_step);
    }

    /// <summary>
    /// Opens the combo window (allowing the next step to be triggered).
    /// </summary>
    public void OpenWindow() => _windowOpen = true;

    /// <summary>
    /// Closes the combo window (preventing the next step from being triggered).
    /// </summary>
    public void CloseWindow() => _windowOpen = false;

    /// <summary>
    /// Returns true if the combo should be reset because too much time has passed since the last step.
    /// </summary>
    public bool ShouldResetByTimeout()
    {
        return _step > 0 && Time.time - _lastAttackTime > _resetTime;
    }

    /// <summary>
    /// Accumulates fallback time. Returns true if the failsafe threshold (<paramref name="failsafeSeconds"/>) has been exceeded.
    /// </summary>
    public bool TickFallback(float failsafeSeconds, bool isAttacking)
    {
        if (isAttacking)
        {
            _fallbackTimer += Time.deltaTime;
            
            if (_fallbackTimer > failsafeSeconds)
                return true;
        }
        else
        {
            _fallbackTimer = 0f;
        }

        return false;
    }

    /// <summary>
    /// Resets the combo system to its initial state.
    /// </summary>
    public void Reset()
    {
        _step = 0;
        _windowOpen = false;
        _fallbackTimer = 0f;
    }

#endregion
}

