using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyVisualFeedback))]
public class Enemy : MonoBehaviour
{
    public enum EnemyState { Patrol, Alert, Attack, Idle }

    [SerializeField] private EnemySettings settings = new EnemySettings();
    public EnemySettings Settings => settings;

    [Header("Attack Strategy")]
    [Tooltip("The attack behaviour component. Drag any MonoBehaviour that implements IEnemyAttackStrategy.")]
    [SerializeField] private MonoBehaviour attackStrategyComponent;

    [Header("State Tracker")]
    public EnemyState currentState = EnemyState.Patrol;

    public Health Health { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public Health PlayerHealth { get; private set; }

    public MeshRenderer MeshRenderer { get; private set; }
    public Color OriginalColor { get; private set; }
    public EnemyVisualFeedback VisualFeedback { get; private set; }
    public float AttackStateProgress => attackStateMachine?.StateProgress ?? 0f;

    private float stateTimer;
    private bool isAware;
    private EnemyAttackStateMachine attackStateMachine;
    private IEnemyAttackStrategy attackStrategy;

    private void Awake()
    {
        Health = GetComponent<Health>();
        Agent = GetComponent<NavMeshAgent>();
        MeshRenderer = GetComponentInChildren<MeshRenderer>();
        VisualFeedback = GetComponent<EnemyVisualFeedback>();
        VisualFeedback.Initialize(MeshRenderer);
        OriginalColor = VisualFeedback.BaseColor;

        attackStateMachine = new EnemyAttackStateMachine();
        ResolveAttackStrategy();
        ApplySettings();

        Health.OnDeath.AddListener(HandleDeath);
        Health.OnDamageTaken.AddListener(HandleDamageTaken);

        if (GetComponent<EnemyUIAutoSetup>() == null)
            gameObject.AddComponent<EnemyUIAutoSetup>();
    }
    private void ResolveAttackStrategy()
    {
        if (attackStrategyComponent == null)
        {
            Debug.LogError($"[{name}] No attack strategy assigned! Add a MonoBehaviour component (e.g. MeleeAttack, DashAttack, RangedAttack) and drag it into the 'Attack Strategy' slot.", this);
            return;
        }

        attackStrategy = attackStrategyComponent as IEnemyAttackStrategy;
        if (attackStrategy == null)
        {
            Debug.LogError($"[{name}] Assigned strategy '{attackStrategyComponent.GetType().Name}' does not implement IEnemyAttackStrategy.", this);
        }
    }
    public void ApplySettings()
    {
        Health.maxHealth = settings.maxHealth;
        Health.currentHealth = settings.maxHealth;
        Agent.speed = settings.speed;

        float executeDuration = attackStrategy?.ExecutingDuration ?? 0f;
        attackStateMachine.Initialize(
            attackStrategy?.ApproachDuration ?? 0f,
            settings.windupDuration,
            executeDuration,
            settings.attackCooldown,
            settings.attackRange,
            settings.alertRange,
            settings.chaseLeashMultiplier
        );
    }

    private void Start()
    {
        var playerObject = FindFirstObjectByType<PlayerFSM>();
        if (playerObject != null)
        {
            PlayerTransform = playerObject.transform;
            PlayerHealth = playerObject.GetComponent<Health>();
        }

        if (gameObject.layer == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) gameObject.layer = enemyLayer;
        }

        GetNewPatrolTarget();
    }

    private void Update()
    {
        if (PlayerTransform == null || !PlayerTransform.gameObject.activeInHierarchy)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                PlayerTransform = playerObject.transform;
                PlayerHealth = playerObject.GetComponent<Health>();
            }
        }

        if (PlayerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
        ExecuteBehaviorLoop(distanceToPlayer);
    }

    private void ExecuteBehaviorLoop(float distanceToPlayer)
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolBehavior();
                if (distanceToPlayer <= settings.alertRange)
                {
                    isAware = true;
                    ChangeState(EnemyState.Alert);
                }
                break;

            case EnemyState.Alert:
                stateTimer -= Time.deltaTime;
                if (Mathf.FloorToInt(stateTimer * 10) % 2 == 0)
                    FlashColor(Color.yellow, 0.1f);

                if (stateTimer <= 0)
                    ChangeState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                if (attackStrategy == null) return;

                EnemyAttackStateMachine.State previousAttackState = attackStateMachine.CurrentState;
                bool stillEngaged = attackStateMachine.Tick(
                    distanceToPlayer,
                    attackStrategy.IsExecutionComplete,
                    attackStrategy.BeginsWindupAtAnyDistance,
                    settings.awarenessIsPermanent && isAware);
                if (!stillEngaged)
                {
                    ChangeState(EnemyState.Idle);
                    break;
                }

                if (attackStateMachine.CurrentState != previousAttackState)
                    NotifyAttackStateExit(previousAttackState);

                switch (attackStateMachine.CurrentState)
                {
                    case EnemyAttackStateMachine.State.Approaching:
                        attackStrategy.OnApproachTarget(this, distanceToPlayer);
                        break;
                    case EnemyAttackStateMachine.State.Windup:
                        attackStrategy.OnWindup(this);
                        break;
                    case EnemyAttackStateMachine.State.Executing:
                        attackStrategy.OnExecute(this, distanceToPlayer);
                        break;
                    case EnemyAttackStateMachine.State.Cooldown:
                        attackStrategy.OnCooldown(this, distanceToPlayer);
                        break;
                }
                break;

            case EnemyState.Idle:
                stateTimer -= Time.deltaTime;
                if (settings.awarenessIsPermanent && isAware)
                    ChangeState(EnemyState.Attack);
                else if (distanceToPlayer <= settings.alertRange)
                {
                    isAware = true;
                    ChangeState(EnemyState.Alert);
                }
                else if (stateTimer <= 0)
                    ChangeState(EnemyState.Patrol);
                break;
        }
    }

    public void ChangeState(EnemyState newState)
    {
        if (currentState == EnemyState.Attack)
            NotifyAttackStateExit(attackStateMachine.CurrentState);

        VisualFeedback.ClearTelegraph();
        currentState = newState;

        if (newState == EnemyState.Alert)
        {
            Agent.ResetPath();
            stateTimer = settings.alertDuration;
            FlashColor(Color.yellow, 0.5f);
        }
        else if (newState == EnemyState.Idle)
        {
            Agent.ResetPath();
            stateTimer = settings.idleDuration;
        }
        else if (newState == EnemyState.Patrol)
        {
            GetNewPatrolTarget();
        }
        else if (newState == EnemyState.Attack)
        {
            attackStateMachine.Reset();
        }
    }

    private void NotifyAttackStateExit(EnemyAttackStateMachine.State state)
    {
        VisualFeedback.ClearTelegraph();
        if (attackStrategy is IEnemyAttackStateExitHandler handler)
            handler.OnAttackStateExit(this, state);
    }

    private void PatrolBehavior()
    {
        Agent.speed = settings.PatrolSpeed;

        if (!Agent.pathPending && Agent.remainingDistance < 0.5f)
            ChangeState(EnemyState.Idle);
    }

    private void GetNewPatrolTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * settings.roamRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, settings.roamRadius, 1))
        {
            Agent.SetDestination(hit.position);
        }
    }

    public void MoveTowards(Vector3 target, float moveSpeed)
    {
        Agent.speed = moveSpeed;
        Agent.SetDestination(target);
    }

    public void MoveAwayFrom(Vector3 target, float moveSpeed)
    {
        Vector3 direction = (transform.position - target).normalized;
        Vector3 fleePosition = transform.position + direction * 5f;

        if (NavMesh.SamplePosition(fleePosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            Agent.speed = moveSpeed;
            Agent.SetDestination(hit.position);
        }
    }

    public void FlashColor(Color color, float duration = 0.1f)
    {
        VisualFeedback.SetTelegraph(color, duration);
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} killed.");
        Destroy(gameObject, 0.1f);
    }

    private void HandleDamageTaken(float damageAmount)
    {
        if (!settings.awarenessIsPermanent)
            return;

        isAware = true;

        if (currentState != EnemyState.Attack)
            ChangeState(EnemyState.Attack);
    }

    private void OnDestroy()
    {
        if (Health == null)
            return;

        Health.OnDeath.RemoveListener(HandleDeath);
        Health.OnDamageTaken.RemoveListener(HandleDamageTaken);
    }

    private void OnValidate()
    {
        settings ??= new EnemySettings();
        if (attackStrategyComponent != null && !(attackStrategyComponent is IEnemyAttackStrategy))
            Debug.LogWarning($"[{name}] Assigned strategy '{attackStrategyComponent.GetType().Name}' does not implement IEnemyAttackStrategy.", this);

    }
}
