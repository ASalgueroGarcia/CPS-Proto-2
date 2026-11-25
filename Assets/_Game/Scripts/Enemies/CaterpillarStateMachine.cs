using System;
using UnityEngine;

/// <summary>
/// Pure C# state machine driving the Caterpillar's awareness, chase, cooldown hold,
/// windup, forward slam, head-only vulnerable split, reform, and death rules.
/// </summary>
public class CaterpillarStateMachine
{
    private const float TimerEpsilon = 0.00001f;

    private readonly CaterpillarSettings settings;
    private float phaseElapsed;
    private bool hasSlamTarget;

    public CaterpillarSettings Settings => settings;
    public CaterpillarPhase Phase { get; private set; } = CaterpillarPhase.Patrol;

    public bool IsAware { get; private set; }
    public bool EyesAlert { get; private set; }
    public bool PiecesScattered { get; private set; }
    public bool DiedWhileSplit { get; private set; }
    public bool ImpactTriggeredThisTick { get; private set; }

    public Vector3 TrackedDirection { get; private set; } = Vector3.forward;
    public Vector3 LockedSlamDirection { get; private set; } = Vector3.forward;
    public Vector3 SlamImpactCenter { get; private set; }

    public float WindupProgress { get; private set; }
    public float SlamProgress { get; private set; }
    public float SplitProgress { get; private set; }
    public float ReformProgress { get; private set; }
    public float CooldownRemaining { get; private set; }

    public float CurrentHealth { get; private set; }
    public float MaxHealth => settings.maxHealth;
    public float SlamDuration => settings.SlamDuration;

    public bool CanMove => Phase == CaterpillarPhase.Patrol || Phase == CaterpillarPhase.Chase;

    public bool CanAttack =>
        Phase != CaterpillarPhase.Dead &&
        Phase != CaterpillarPhase.Split &&
        Phase != CaterpillarPhase.Reform &&
        Phase != CaterpillarPhase.Hold &&
        CooldownRemaining <= TimerEpsilon;

    public bool IsHeadOnlyVulnerable => Phase == CaterpillarPhase.Split;

    public Vector3 DesiredVelocity =>
        Phase == CaterpillarPhase.Chase ? TrackedDirection * settings.moveSpeed : Vector3.zero;

    public event Action<Vector3> OnSlamImpact;

    public CaterpillarStateMachine(CaterpillarSettings settings, Vector3 initialForward = default)
    {
        this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        Reset(initialForward);
    }

    public void Reset(Vector3 initialForward = default)
    {
        Vector3 forward = NormalizeHorizontal(initialForward, Vector3.forward);

        Phase = CaterpillarPhase.Patrol;
        IsAware = false;
        EyesAlert = false;
        PiecesScattered = false;
        DiedWhileSplit = false;
        ImpactTriggeredThisTick = false;
        hasSlamTarget = false;

        TrackedDirection = forward;
        LockedSlamDirection = forward;
        SlamImpactCenter = Vector3.zero;

        WindupProgress = 0f;
        SlamProgress = 0f;
        SplitProgress = 0f;
        ReformProgress = 0f;
        CooldownRemaining = 0f;
        phaseElapsed = 0f;
        CurrentHealth = settings.maxHealth;
    }

    public void Tick(Vector3 currentPosition, Vector3 playerPosition, float deltaTime)
    {
        ImpactTriggeredThisTick = false;

        if (Phase == CaterpillarPhase.Dead)
            return;

        if (CurrentHealth <= 0f)
        {
            OnDeath();
            return;
        }

        deltaTime = Mathf.Max(0f, deltaTime);
        float distanceToPlayer = HorizontalDistance(currentPosition, playerPosition);

        switch (Phase)
        {
            case CaterpillarPhase.Patrol:
                if (distanceToPlayer <= settings.detectionRange)
                {
                    IsAware = true;
                    UpdateTrackedDirection(currentPosition, playerPosition);
                    EngagePlayer(currentPosition, playerPosition, distanceToPlayer);
                }
                break;

            case CaterpillarPhase.Chase:
                UpdateTrackedDirection(currentPosition, playerPosition);
                AdvanceCooldown(deltaTime);
                if (distanceToPlayer <= settings.attackRange)
                {
                    if (CooldownRemaining <= TimerEpsilon)
                        EnterWindup(currentPosition, playerPosition);
                    else
                        EnterHold();
                }
                break;

            case CaterpillarPhase.Hold:
                UpdateTrackedDirection(currentPosition, playerPosition);
                AdvanceCooldown(deltaTime);
                if (distanceToPlayer > settings.attackRange)
                    EnterChase();
                else if (CooldownRemaining <= TimerEpsilon)
                    EnterWindup(currentPosition, playerPosition);
                break;

            case CaterpillarPhase.Windup:
                UpdateTrackedDirection(currentPosition, playerPosition);
                phaseElapsed += deltaTime;
                WindupProgress = settings.windupDuration > TimerEpsilon
                    ? Mathf.Clamp01(phaseElapsed / settings.windupDuration)
                    : 1f;

                if (phaseElapsed + TimerEpsilon >= settings.windupDuration)
                {
                    WindupProgress = 1f;
                    LockSlamTarget(currentPosition);
                    EnterSlam(currentPosition, playerPosition);
                }
                break;

            case CaterpillarPhase.Slam:
                phaseElapsed += deltaTime;
                SlamProgress = SlamDuration > TimerEpsilon
                    ? Mathf.Clamp01(phaseElapsed / SlamDuration)
                    : 1f;

                if (phaseElapsed + TimerEpsilon >= SlamDuration)
                {
                    SlamProgress = 1f;
                    TriggerSlamImpact();
                    EnterSplit(currentPosition, playerPosition);
                }
                break;

            case CaterpillarPhase.Split:
                phaseElapsed += deltaTime;
                SplitProgress = settings.splitDuration > TimerEpsilon
                    ? Mathf.Clamp01(phaseElapsed / settings.splitDuration)
                    : 1f;

                if (phaseElapsed + TimerEpsilon >= settings.splitDuration)
                {
                    SplitProgress = 1f;
                    EnterReform(currentPosition, playerPosition);
                }
                break;

            case CaterpillarPhase.Reform:
                phaseElapsed += deltaTime;
                ReformProgress = settings.reformDuration > TimerEpsilon
                    ? Mathf.Clamp01(phaseElapsed / settings.reformDuration)
                    : 1f;

                if (phaseElapsed + TimerEpsilon >= settings.reformDuration)
                    CompleteReform(currentPosition, playerPosition);
                break;
        }
    }

    /// <summary>True when point lies within slamImpactRadius of SlamImpactCenter on the horizontal plane.</summary>
    public bool IsPointInSlamImpact(Vector3 point)
    {
        if (!hasSlamTarget)
            return false;

        return HorizontalDistance(SlamImpactCenter, point) <= settings.slamImpactRadius;
    }

    /// <summary>Advances currentPosition horizontally toward playerPosition while in Chase.</summary>
    public Vector3 MoveStep(Vector3 currentPosition, Vector3 playerPosition, float deltaTime)
    {
        if (Phase != CaterpillarPhase.Chase || deltaTime <= 0f)
            return currentPosition;

        Vector3 flatTarget = new Vector3(playerPosition.x, currentPosition.y, playerPosition.z);
        return Vector3.MoveTowards(currentPosition, flatTarget, settings.moveSpeed * deltaTime);
    }

    /// <summary>During Split only the head piece accepts damage; while Dead nothing does.</summary>
    public bool CanDamagePiece(bool isHeadPiece)
    {
        if (Phase == CaterpillarPhase.Dead)
            return false;

        return !IsHeadOnlyVulnerable || isHeadPiece;
    }

    /// <summary>Applies damage respecting head-only split vulnerability and triggers death at 0 HP.</summary>
    public bool ApplyDamage(float amount, bool isHeadPiece = true)
    {
        if (amount <= 0f || !CanDamagePiece(isHeadPiece))
            return false;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (CurrentHealth <= 0f)
            OnDeath();

        return true;
    }

    public void SetHealth(float health)
    {
        if (Phase == CaterpillarPhase.Dead)
            return;

        CurrentHealth = Mathf.Clamp(health, 0f, settings.maxHealth);
        if (CurrentHealth <= 0f)
            OnDeath();
    }

    public void OnDeath()
    {
        if (Phase == CaterpillarPhase.Dead)
            return;

        if (PiecesScattered || Phase == CaterpillarPhase.Split)
        {
            DiedWhileSplit = true;
            PiecesScattered = true;
        }
        else
        {
            DiedWhileSplit = false;
            PiecesScattered = false;
        }

        CurrentHealth = 0f;
        EyesAlert = false;
        Phase = CaterpillarPhase.Dead;
    }

    public static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = b.x - a.x;
        float dz = b.z - a.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private void EngagePlayer(Vector3 currentPosition, Vector3 playerPosition, float distanceToPlayer)
    {
        if (distanceToPlayer <= settings.attackRange)
        {
            if (CooldownRemaining <= TimerEpsilon)
                EnterWindup(currentPosition, playerPosition);
            else
                EnterHold();
        }
        else
        {
            EnterChase();
        }
    }

    private void EnterChase()
    {
        Phase = CaterpillarPhase.Chase;
        phaseElapsed = 0f;
    }

    private void EnterHold()
    {
        Phase = CaterpillarPhase.Hold;
        phaseElapsed = 0f;
    }

    private void EnterWindup(Vector3 currentPosition, Vector3 playerPosition)
    {
        Phase = CaterpillarPhase.Windup;
        EyesAlert = true;
        phaseElapsed = 0f;

        if (settings.windupDuration <= TimerEpsilon)
        {
            WindupProgress = 1f;
            LockSlamTarget(currentPosition);
            EnterSlam(currentPosition, playerPosition);
            return;
        }

        WindupProgress = 0f;
    }

    private void LockSlamTarget(Vector3 currentPosition)
    {
        LockedSlamDirection = TrackedDirection;
        SlamImpactCenter = currentPosition + LockedSlamDirection * settings.slamCenterOffset;
        hasSlamTarget = true;
    }

    private void EnterSlam(Vector3 currentPosition, Vector3 playerPosition)
    {
        phaseElapsed = 0f;

        if (SlamDuration <= TimerEpsilon)
        {
            SlamProgress = 1f;
            TriggerSlamImpact();
            EnterSplit(currentPosition, playerPosition);
            return;
        }

        Phase = CaterpillarPhase.Slam;
        SlamProgress = 0f;
    }

    private void TriggerSlamImpact()
    {
        ImpactTriggeredThisTick = true;
        OnSlamImpact?.Invoke(SlamImpactCenter);
    }

    private void EnterSplit(Vector3 currentPosition, Vector3 playerPosition)
    {
        Phase = CaterpillarPhase.Split;
        PiecesScattered = true;
        phaseElapsed = 0f;

        if (settings.splitDuration <= TimerEpsilon)
        {
            SplitProgress = 1f;
            EnterReform(currentPosition, playerPosition);
            return;
        }

        SplitProgress = 0f;
    }

    private void EnterReform(Vector3 currentPosition, Vector3 playerPosition)
    {
        Phase = CaterpillarPhase.Reform;
        PiecesScattered = true;
        phaseElapsed = 0f;

        if (settings.reformDuration <= TimerEpsilon)
        {
            CompleteReform(currentPosition, playerPosition);
            return;
        }

        ReformProgress = 0f;
    }

    private void CompleteReform(Vector3 currentPosition, Vector3 playerPosition)
    {
        ReformProgress = 1f;
        EyesAlert = false;
        PiecesScattered = false;
        CooldownRemaining = Mathf.Max(0f, settings.attackCooldown);
        UpdateTrackedDirection(currentPosition, playerPosition);

        float distanceToPlayer = HorizontalDistance(currentPosition, playerPosition);
        EngagePlayer(currentPosition, playerPosition, distanceToPlayer);
    }

    private void AdvanceCooldown(float deltaTime)
    {
        if (CooldownRemaining <= 0f)
            return;

        CooldownRemaining = Mathf.Max(0f, CooldownRemaining - deltaTime);
        if (CooldownRemaining <= TimerEpsilon)
            CooldownRemaining = 0f;
    }

    private void UpdateTrackedDirection(Vector3 currentPosition, Vector3 playerPosition)
    {
        Vector3 toPlayer = playerPosition - currentPosition;
        TrackedDirection = NormalizeHorizontal(toPlayer, TrackedDirection);
    }

    private static Vector3 NormalizeHorizontal(Vector3 vector, Vector3 fallback)
    {
        vector.y = 0f;
        if (vector.sqrMagnitude > 0.0001f)
            return vector.normalized;

        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
    }
}
