using System;
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
/// Generic combo system. Holds combo state (step counter and the grace period timer)
/// without knowing about any specific weapon. The weapon is responsible for mapping a
/// given step index to its own damage / color / animation result.
///
/// Two clocks, and only two:
/// - the attack cooldown, the minimum time between two attacks;
/// - the grace period, the maximum time without attacking before the combo is lost.
///
/// The grace period is measured from the last chained step, not from the start of the
/// chain, so a player who keeps the rhythm keeps the combo for as long as they want.
/// </summary>
public class WeaponComboSystem
{

#region Fields and Properties

    private int _step = 0;
    private float _lastAttackTime = 0f;
    private readonly float _resetTime;
    private readonly float _attackCooldown;
    private readonly int _maxSteps;

    /// <summary>
    /// Clock used by every deadline in this class. Injected so tests can drive the combo
    /// timeline deterministically instead of depending on the engine clock, which does not
    /// advance in EditMode tests.
    /// </summary>
    private readonly Func<float> _now;

    public int CurrentStep => _step;

    public float LastAttackTime => _lastAttackTime;
    public float TimeSinceLastStep => _step > 0 ? _now() - _lastAttackTime : Mathf.Infinity;
    public int MaxSteps => _maxSteps;
    public float AttackCooldown => _attackCooldown;

    /// <summary>True when no combo is active or the cooldown has elapsed since the last attack.</summary>
    public bool CanAttack => _step == 0 || (_now() - _lastAttackTime) >= _attackCooldown;

    /// <summary>Seconds remaining until the next attack is allowed. Returns 0 when ready.</summary>
    public float AttackCooldownRemaining => Mathf.Max(0f, _attackCooldown - (_now() - _lastAttackTime));

#endregion

    /// <param name="resetTime">Grace period, in seconds, to chain the next step before the combo is lost.</param>
    /// <param name="attackCooldown">Minimum seconds between two attacks.</param>
    /// <param name="maxSteps">Number of steps in the cycle.</param>
    /// <param name="now">Clock source; defaults to the engine clock.</param>
    public WeaponComboSystem(float resetTime, float attackCooldown, int maxSteps = 3, Func<float> now = null)
    {
        _resetTime = Mathf.Max(0.01f, resetTime);
        _attackCooldown = Mathf.Max(0f, attackCooldown);
        _maxSteps = Mathf.Max(1, maxSteps);
        _now = now ?? (() => Time.time);
    }

#region Public Methods

    /// <summary>
    /// Advances to the next step, wrapping at <see cref="MaxSteps"/> (e.g. 1,2,3,1,2,3,...).
    /// Caller maps the returned step index to its own result data.
    /// </summary>
    public ComboStep AdvanceStep()
    {
        _step = (_step % _maxSteps) + 1;
        _lastAttackTime = _now();
        return new ComboStep(_step);
    }

    /// <summary>
    /// Returns true if the combo should be reset because too much time has passed since the last step.
    /// This is the only clock that can end a combo on its own: it is the grace period the
    /// player gets to chain the next step before the chain is lost.
    /// </summary>
    public bool ShouldResetByTimeout()
    {
        return _step > 0 && _now() - _lastAttackTime > _resetTime;
    }

    /// <summary>
    /// Resets the combo system to its initial state.
    /// </summary>
    public void Reset()
    {
        _step = 0;
        // Clearing the timestamp keeps LastAttackTime honest: a reset combo has no
        // last attack, so the idle/heavy logic must not read a stale one.
        _lastAttackTime = 0f;
    }

#endregion
}
