using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    public enum EnemyState { Patrol, Alert, Attack, Idle }

    [Header("Configuration")]
    [Tooltip("Data asset containing all tunable stats for this enemy.")]
    public EnemyData data;

    [Header("Attack Strategy")]
    [Tooltip("The attack behaviour component. Drag any MonoBehaviour that implements IEnemyAttackStrategy.")]
    [SerializeField] private MonoBehaviour attackStrategyComponent;

    [Header("State Tracker")]
    public EnemyState currentState = EnemyState.Patrol;

    // Cached references (exposed as read-only for strategies)
    public Health Health { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public Health PlayerHealth { get; private set; }

    public MeshRenderer MeshRenderer { get; private set; }
    public Color OriginalColor { get; private set; }

    // Internal
    private float stateTimer;
    private EnemyAttackStateMachine attackSM;
    private IEnemyAttackStrategy attackStrategy;

    // Patrol
    private Vector3 patrolTarget;
    private bool isMovingToPatrolPoint;

    private void Awake()
    {
        Health = GetComponent<Health>();
        Agent = GetComponent<NavMeshAgent>();
        MeshRenderer = GetComponentInChildren<MeshRenderer>();
        if (MeshRenderer != null) OriginalColor = MeshRenderer.material.color;

        attackSM = new EnemyAttackStateMachine();
        ResolveAttackStrategy();
        ApplyDataStats();

        Health.OnDeath.AddListener(HandleDeath);

        if (GetComponent<EnemyUIAutoSetup>() == null)
            gameObject.AddComponent<EnemyUIAutoSetup>();
    }

    /// <summary>
    /// Extracts the IEnemyAttackStrategy from the assigned MonoBehaviour slot.
    /// Validates in Editor via OnValidate; validates at runtime here.
    /// </summary>
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

    /// <summary>
    /// Applies stats from the assigned EnemyData asset.
    /// Call at runtime if you swap data assets (e.g., difficulty scaling).
    /// </summary>
    public void ApplyDataStats()
    {
        if (data == null)
        {
            Debug.LogError($"[{name}] No EnemyData assigned! Create an EnemyData asset (Assets > Create > Enemies > Enemy Data) and assign it.", this);
            return;
        }

        Health.maxHealth = data.maxHealth;
        Health.currentHealth = data.maxHealth;
        Agent.speed = data.speed;

        float executeDuration = attackStrategy?.ExecutingDuration ?? 0f;
        attackSM.Initialize(
            data.windupDuration,
            executeDuration,
            data.attackCooldown,
            data.attackRange,    // <-- attack trigger distance (Approaching→Windup)
            data.alertRange,     // <-- leash base distance
            data.chaseLeashMultiplier
        );
    }

    private void Start()
    {
        var playerObj = FindFirstObjectByType<PlayerFSM>();
        if (playerObj != null)
        {
            PlayerTransform = playerObj.transform;
            PlayerHealth = playerObj.GetComponent<Health>();
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
        // Recover player reference if lost (scene loading, respawn, etc.)
        if (PlayerTransform == null || !PlayerTransform.gameObject.activeInHierarchy)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                PlayerTransform = playerObj.transform;
                PlayerHealth = playerObj.GetComponent<Health>();
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
                if (distanceToPlayer <= data.alertRange)
                    ChangeState(EnemyState.Alert);
                break;

            case EnemyState.Alert:
                stateTimer -= Time.deltaTime;
                if (Mathf.FloorToInt(stateTimer * 10) % 2 == 0)
                    FlashColor(Color.yellow, 0.1f);

                if (stateTimer <= 0)
                    ChangeState(EnemyState.Attack);
                break;

            case EnemyState.Attack:
                if (attackStrategy == null) return; // Graceful degradation if strategy missing

                bool stillEngaged = attackSM.Tick(distanceToPlayer);
                if (!stillEngaged)
                {
                    ChangeState(EnemyState.Idle);
                    break;
                }

                switch (attackSM.CurrentState)
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
                if (distanceToPlayer <= data.alertRange)
                    ChangeState(EnemyState.Alert);
                else if (stateTimer <= 0)
                    ChangeState(EnemyState.Patrol);
                break;
        }
    }

    public void ChangeState(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Alert)
        {
            Agent.ResetPath();
            stateTimer = data.alertDuration;
            FlashColor(Color.yellow, 0.5f);
        }
        else if (newState == EnemyState.Idle)
        {
            Agent.ResetPath();
            stateTimer = data.idleDuration;
        }
        else if (newState == EnemyState.Patrol)
        {
            GetNewPatrolTarget();
        }
        else if (newState == EnemyState.Attack)
        {
            attackSM.Reset();
        }
    }

    private void PatrolBehavior()
    {
        Agent.speed = data.speed * data.patrolSpeedMultiplier;

        if (!Agent.pathPending && Agent.remainingDistance < 0.5f)
            ChangeState(EnemyState.Idle);
    }

    private void GetNewPatrolTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * data.roamRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, data.roamRadius, 1))
        {
            patrolTarget = hit.position;
            Agent.SetDestination(patrolTarget);
            isMovingToPatrolPoint = true;
        }
    }

    // --- Public utilities used by strategies ---

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
        if (MeshRenderer != null)
        {
            MeshRenderer.material.color = color;
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), duration);
        }
    }

    private void ResetColor()
    {
        if (MeshRenderer != null) MeshRenderer.material.color = OriginalColor;
    }

    private void HandleDeath()
    {
        Debug.Log($"{gameObject.name} killed.");
        Destroy(gameObject, 0.1f);
    }

    private void OnValidate()
    {
        if (data != null && Agent != null)
            Agent.speed = data.speed;

        // Warn in Inspector if assigned component doesn't implement the interface
        if (attackStrategyComponent != null && !(attackStrategyComponent is IEnemyAttackStrategy))
            Debug.LogWarning($"[{name}] Assigned strategy '{attackStrategyComponent.GetType().Name}' does not implement IEnemyAttackStrategy.", this);

        // [SerializeField] MonoBehaviour slot: the field must be on the same GameObject
        // — Unity will only serialize MonoBehaviour refs to components on the prefab.
    }
}
