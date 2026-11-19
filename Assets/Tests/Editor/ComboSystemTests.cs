using NUnit.Framework;

/// <summary>
/// Covers the combo grace period and the attack cooldown.
///
/// The bug these guard against: the combo grace period used to be the only clock, but the
/// attack animation's ReturnToIdle event cleared the whole chain on its own, so no matter
/// what the Inspector said, every chain collapsed back to step 1.
///
/// The clock is injected so the timeline is deterministic; the engine clock does not advance
/// in EditMode tests.
/// </summary>
public class ComboSystemTests
{
    private const float ResetTime = 1.5f;
    private const float AttackCooldown = 0.15f;
    private const int MaxSteps = 3;

    private float _now;
    private WeaponComboSystem _combo;

    [SetUp]
    public void SetUp()
    {
        _now = 0f;
        _combo = new WeaponComboSystem(ResetTime, AttackCooldown, MaxSteps, () => _now);
    }

    private void Advance(float seconds) => _now += seconds;

    [Test]
    public void Combo_CyclesThroughItsSteps_AndWrapsAround()
    {
        Assert.AreEqual(1, _combo.AdvanceStep().Index);
        Assert.AreEqual(2, _combo.AdvanceStep().Index);
        Assert.AreEqual(3, _combo.AdvanceStep().Index);
        Assert.AreEqual(1, _combo.AdvanceStep().Index, "The combo is a cycle, not a chain.");
    }

    [Test]
    public void Combo_SurvivesChaining_AsLongAsTheGracePeriodHasNotElapsed()
    {
        _combo.AdvanceStep();
        Advance(ResetTime - 0.1f);

        _combo.AdvanceStep();

        Assert.AreEqual(2, _combo.CurrentStep, "The chain must survive inside the grace period.");
        Assert.IsFalse(_combo.ShouldResetByTimeout());
    }

    [Test]
    public void Combo_IsLost_WhenTheGracePeriodElapses_WithoutChaining()
    {
        _combo.AdvanceStep();
        Advance(ResetTime + 0.01f);

        Assert.IsTrue(_combo.ShouldResetByTimeout(), "Abandoning the combo must end it.");
    }

    [Test]
    public void GracePeriod_IsMeasuredFromTheLastChainedStep_NotFromTheFirst()
    {
        _combo.AdvanceStep();
        Advance(ResetTime - 0.2f);
        _combo.AdvanceStep();
        Advance(ResetTime - 0.2f);

        Assert.IsFalse(
            _combo.ShouldResetByTimeout(),
            "Each chained step must refresh the grace period, otherwise a long chain still dies.");
    }

    [Test]
    public void GracePeriod_KeepsTheComboAlive_ForAsLongAsTheRhythmIsKept()
    {
        for (int i = 0; i < 20; i++)
        {
            _combo.AdvanceStep();
            Advance(ResetTime - 0.05f);
            Assert.IsFalse(_combo.ShouldResetByTimeout(), $"The chain died at step {i + 1}.");
        }
    }

    [Test]
    public void Reset_ClearsTheStepAndTheLastAttackTime()
    {
        _combo.AdvanceStep();
        Advance(0.5f);

        _combo.Reset();

        Assert.AreEqual(0, _combo.CurrentStep);
        Assert.AreEqual(0f, _combo.LastAttackTime, "A reset combo has no last attack to report.");
    }

    [Test]
    public void CanAttack_IsBlockedByTheCooldown_ButTheFirstAttackIsAlwaysAllowed()
    {
        Assert.IsTrue(_combo.CanAttack, "The first attack is never on cooldown.");

        _combo.AdvanceStep();
        Assert.IsFalse(_combo.CanAttack);

        Advance(AttackCooldown + 0.01f);
        Assert.IsTrue(_combo.CanAttack);
    }

    [Test]
    public void CanAttack_IsAllowedAgain_AfterAReset_EvenWithoutWaitingForTheCooldown()
    {
        _combo.AdvanceStep();
        _combo.Reset();

        Assert.IsTrue(_combo.CanAttack);
    }

    [Test]
    public void AResetCombo_DoesNotTimeOut_OnItsOwn()
    {
        _combo.AdvanceStep();
        _combo.Reset();
        Advance(ResetTime * 10f);

        Assert.IsFalse(_combo.ShouldResetByTimeout(), "There is no combo left to time out.");
    }

    [Test]
    public void TimeSinceLastStep_ReportsTheGapBetweenAttacks()
    {
        _combo.AdvanceStep();
        Advance(0.4f);

        Assert.AreEqual(0.4f, _combo.TimeSinceLastStep, 0.001f);
    }

    [Test]
    public void TimeSinceLastStep_IsInfinite_WhenNoComboIsActive()
    {
        Assert.IsTrue(float.IsInfinity(_combo.TimeSinceLastStep));
    }
}
