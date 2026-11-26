using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Runtime attack strategy for the Caterpillar enemy. Drives CaterpillarStateMachine from
/// CaterpillarSettings, coordinates NavMesh chase/stop, deals slam AoE damage and knockback,
/// enforces head-only Split hitboxes, and detaches scattered remains on split death.
/// </summary>
[RequireComponent(typeof(CaterpillarSegmentVisuals))]
public class CaterpillarAttack : MonoBehaviour, IEnemyAttackStrategy, IEnemyAttackStateExitHandler
{
    [Header("Configuration")]
    [SerializeField] private CaterpillarSettings settings;

    [Header("Visuals")]
    [SerializeField] private CaterpillarSegmentVisuals segmentVisuals;

    private Enemy enemy;
    private Health health;
    private NavMeshAgent agent;
    private Collider rootCollider;
    private CaterpillarStateMachine stateMachine;
    private CaterpillarSettings runtimeFallbackSettings;

    private Health activePlayerHealth;
    private Vector3 activePlayerPosition;
    private bool initialized;
    private bool isDead;
    private bool steppedThisFrame;

    public CaterpillarSettings Settings => settings;
    public CaterpillarStateMachine StateMachine => stateMachine;
    public CaterpillarSegmentVisuals SegmentVisuals => segmentVisuals;
    public CaterpillarPhase CurrentPhase => stateMachine != null ? stateMachine.Phase : CaterpillarPhase.Patrol;
    public GameObject DetachedRemainsRoot => segmentVisuals != null ? segmentVisuals.DetachedRemainsRoot : null;

    public float ApproachDuration => 0f;
    public float ExecutingDuration => 1000000f;
    public bool IsExecutionComplete => false;
    public bool BeginsWindupAtAnyDistance => true;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize(CaterpillarSettings overrideSettings = null)
    {
        if (overrideSettings != null)
            settings = overrideSettings;

        if (initialized && overrideSettings == null)
            return;

        UnsubscribeEvents();

        if (settings == null)
        {
            runtimeFallbackSettings = ScriptableObject.CreateInstance<CaterpillarSettings>();
            settings = runtimeFallbackSettings;
        }

        enemy = GetComponent<Enemy>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        rootCollider = GetComponent<Collider>();
        if (segmentVisuals == null)
            segmentVisuals = GetComponent<CaterpillarSegmentVisuals>();

        if (rootCollider != null)
            rootCollider.enabled = false;

        SyncEnemyAndHealthSettings();

        stateMachine = new CaterpillarStateMachine(settings, transform.forward);
        stateMachine.OnSlamImpact += HandleSlamImpact;

        if (segmentVisuals != null)
        {
            segmentVisuals.Initialize(settings, health, this);

            EnemyVisualFeedback feedback = GetComponent<EnemyVisualFeedback>();
            if (feedback != null && segmentVisuals.HeadRenderer != null)
                feedback.Initialize(segmentVisuals.HeadRenderer);
        }

        if (health != null)
        {
            if (health.OnDamageTaken == null)
                health.OnDamageTaken = new UnityEvent<float>();
            if (health.OnDeath == null)
                health.OnDeath = new UnityEvent();

            health.OnDamageTaken.AddListener(HandleDamageTaken);
            health.OnDeath.AddListener(HandleDeath);
        }

        isDead = false;
        initialized = true;
    }

    public void OnApproachTarget(Enemy owner, float distanceToPlayer) => TickFromOwner(owner);

    public void OnWindup(Enemy owner) => TickFromOwner(owner);

    public void OnExecute(Enemy owner, float distanceToPlayer) => TickFromOwner(owner);

    public void OnCooldown(Enemy owner, float distanceToPlayer) => TickFromOwner(owner);

    public void OnAttackStateExit(Enemy owner, EnemyAttackStateMachine.State state)
    {
    }

    /// <summary>
    /// Advances the Caterpillar state machine, movement, slam AoE check, and sphere-chain visuals
    /// by deltaTime toward playerPosition. Usable both in PlayMode and EditMode tests.
    /// </summary>
    public void Step(Vector3 playerPosition, float deltaTime, Health playerHealth = null)
    {
        if (!initialized)
            Initialize();

        if (isDead || stateMachine == null || stateMachine.Phase == CaterpillarPhase.Dead)
            return;

        steppedThisFrame = true;
        activePlayerPosition = playerPosition;
        activePlayerHealth = playerHealth;

        stateMachine.Tick(transform.position, playerPosition, deltaTime);

        ApplyMovementForPhase(playerPosition, deltaTime);

        if (segmentVisuals != null)
            segmentVisuals.ApplyPhaseVisuals(stateMachine, transform.position, deltaTime);
    }

    /// <summary>Routes piece-specific damage respecting head-only Split vulnerability.</summary>
    public bool TryApplyPieceDamage(float amount, bool isHeadPiece, Vector3 knockbackSource = default, float knockbackForce = 0f)
    {
        if (!initialized)
            Initialize();

        if (amount <= 0f || isDead || stateMachine == null || !stateMachine.CanDamagePiece(isHeadPiece))
            return false;

        if (health != null)
        {
            if (health.isInvulnerable)
                return false;

            health.TakeDamage(amount, knockbackSource, knockbackForce);
            return true;
        }

        bool applied = stateMachine.ApplyDamage(amount, isHeadPiece);
        if (stateMachine.Phase == CaterpillarPhase.Dead)
            HandleDeath();
        return applied;
    }

    private void LateUpdate()
    {
        if (!initialized || isDead || stateMachine == null || segmentVisuals == null)
            return;

        if (!steppedThisFrame)
        {
            if (stateMachine.Phase == CaterpillarPhase.Patrol)
                segmentVisuals.UpdateTrail(transform.position, transform.forward, Time.deltaTime);
            else
                segmentVisuals.ReapplyWorldTransforms();
        }
        else
        {
            segmentVisuals.ReapplyWorldTransforms();
        }

        steppedThisFrame = false;
    }

    private void TickFromOwner(Enemy owner)
    {
        if (owner == null || owner.PlayerTransform == null)
            return;

        Step(owner.PlayerTransform.position, Time.deltaTime, owner.PlayerHealth);
    }

    private void ApplyMovementForPhase(Vector3 playerPosition, float deltaTime)
    {
        switch (stateMachine.Phase)
        {
            case CaterpillarPhase.Chase:
                if (CanUseNavMeshAgent())
                {
                    agent.speed = settings.moveSpeed;
                    agent.SetDestination(playerPosition);
                }
                else
                {
                    transform.position = stateMachine.MoveStep(transform.position, playerPosition, deltaTime);
                }

                FaceDirection(stateMachine.TrackedDirection);
                break;

            case CaterpillarPhase.Hold:
            case CaterpillarPhase.Windup:
            case CaterpillarPhase.Reform:
                StopNavMeshAgent();
                FaceDirection(stateMachine.TrackedDirection);
                break;

            case CaterpillarPhase.Slam:
            case CaterpillarPhase.Split:
                StopNavMeshAgent();
                FaceDirection(stateMachine.LockedSlamDirection);
                break;
        }
    }

    private void HandleSlamImpact(Vector3 impactCenter)
    {
        if (activePlayerHealth == null || settings == null)
            return;

        if (stateMachine.IsPointInSlamImpact(activePlayerPosition))
            activePlayerHealth.TakeDamage(settings.slamDamage, impactCenter, settings.slamKnockbackForce);
    }

    private void HandleDamageTaken(float damageAmount)
    {
        if (isDead || stateMachine == null || health == null)
            return;

        if (stateMachine.Phase == CaterpillarPhase.Patrol && health.currentHealth > 0f)
            WakeUpFromRemoteHit();

        stateMachine.SetHealth(health.currentHealth);
        if (stateMachine.Phase == CaterpillarPhase.Dead)
            HandleDeath();
    }

    private void WakeUpFromRemoteHit()
    {
        Vector3 direction = enemy != null && enemy.PlayerTransform != null
            ? enemy.PlayerTransform.position - transform.position
            : transform.forward;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;

        float chaseProbeDistance = (settings.attackRange + settings.detectionRange) * 0.5f;
        if (chaseProbeDistance <= settings.attackRange)
            chaseProbeDistance = settings.detectionRange;

        stateMachine.Tick(transform.position, transform.position + direction * chaseProbeDistance, 0f);
    }

    private void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;
        StopNavMeshAgent();

        if (stateMachine != null)
            stateMachine.OnDeath();

        bool diedWhileSplit = stateMachine != null && stateMachine.DiedWhileSplit;
        if (segmentVisuals != null)
            segmentVisuals.HandleDeath(diedWhileSplit, transform.parent);
    }

    private void SyncEnemyAndHealthSettings()
    {
        if (enemy != null && enemy.Settings != null)
        {
            EnemySettings enemySettings = enemy.Settings;
            enemySettings.maxHealth = settings.maxHealth;
            enemySettings.damage = settings.slamDamage;
            enemySettings.speed = settings.moveSpeed;
            enemySettings.alertRange = settings.detectionRange;
            enemySettings.alertDuration = 0f;
            enemySettings.awarenessIsPermanent = true;
            enemySettings.attackRange = settings.attackRange;
            enemySettings.windupDuration = settings.windupDuration;
            enemySettings.attackCooldown = settings.attackCooldown;

            if (enemy.Health != null && enemy.Agent != null)
                enemy.ApplySettings();
        }

        if (health != null)
        {
            health.maxHealth = settings.maxHealth;
            health.currentHealth = settings.maxHealth;
        }
    }

    private bool CanUseNavMeshAgent() => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    private void StopNavMeshAgent()
    {
        if (CanUseNavMeshAgent())
            agent.ResetPath();
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void UnsubscribeEvents()
    {
        if (stateMachine != null)
            stateMachine.OnSlamImpact -= HandleSlamImpact;

        if (health != null)
        {
            health.OnDamageTaken?.RemoveListener(HandleDamageTaken);
            health.OnDeath?.RemoveListener(HandleDeath);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (segmentVisuals != null)
            segmentVisuals.CleanupTelegraph();

        if (runtimeFallbackSettings != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeFallbackSettings);
            else
                DestroyImmediate(runtimeFallbackSettings);
        }
    }
}
