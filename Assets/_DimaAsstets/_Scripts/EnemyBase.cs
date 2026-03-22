using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public abstract class EnemyBase : MonoBehaviour
{
    public enum EnemyState { Patrol, Alert, Attack, Idle }
    
    [Header("State Tracker")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Base Settings")]
    public float maxHealth = 50f;
    public float damage = 10f;
    public float speed = 5f;
    public float alertRange = 10f;
    public float alertDuration = 1f;
    public float idleDuration = 1.5f;

    [Header("References")]
    protected Health health;
    protected NavMeshAgent agent;
    protected Transform playerTransform;
    protected Health playerHealth;
    protected MeshRenderer meshRenderer;
    protected Color originalColor;
    
    protected float stateTimer = 0f;

    // Patrol variables
    public float roamRadius = 10f;
    protected Vector3 patrolTarget;
    protected bool isMovingToPatrolPoint = false;

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;
        
        health.maxHealth = maxHealth;
        health.currentHealth = maxHealth;

        agent.speed = speed;

        health.OnDeath.AddListener(HandleDeath);
    }

    protected virtual void HandleDeath()
    {
        Debug.Log($"{gameObject.name} killed.");
        Destroy(gameObject, 0.1f);
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<Health>();
        }

        if (gameObject.layer == 0) 
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) gameObject.layer = enemyLayer;
        }
        
        GetNewPatrolTarget();
    }

    protected virtual void Update()
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        ExecuteBehaviorLoop(distanceToPlayer);
    }

    private void ExecuteBehaviorLoop(float distanceToPlayer)
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolBehavior();
                if (distanceToPlayer <= alertRange)
                {
                    ChangeState(EnemyState.Alert);
                }
                break;

            case EnemyState.Alert:
                stateTimer -= Time.deltaTime;
                // Alert visual indicator: flash yellow
                if (Mathf.FloorToInt(stateTimer * 10) % 2 == 0) FlashColor(Color.yellow, 0.1f);
                
                if (stateTimer <= 0)
                {
                    ChangeState(EnemyState.Attack);
                }
                break;

            case EnemyState.Attack:
                AttackBehavior(distanceToPlayer);
                break;

            case EnemyState.Idle:
                stateTimer -= Time.deltaTime;
                if (distanceToPlayer <= alertRange)
                {
                    ChangeState(EnemyState.Alert);
                }
                else if (stateTimer <= 0)
                {
                    ChangeState(EnemyState.Patrol);
                }
                break;
        }
    }

    public virtual void ChangeState(EnemyState newState)
    {
        currentState = newState;
        
        if (newState == EnemyState.Alert)
        {
            agent.ResetPath(); // Stop moving
            stateTimer = alertDuration;
            // Visual indication of alert
            FlashColor(Color.yellow, 0.5f);
        }
        else if (newState == EnemyState.Idle)
        {
            agent.ResetPath(); // Stop moving
            stateTimer = idleDuration;
        }
        else if (newState == EnemyState.Patrol)
        {
            GetNewPatrolTarget();
        }
    }

    protected virtual void PatrolBehavior()
    {
        agent.speed = speed * 0.5f; // Move slower while patrolling

        // If the enemy reached its destination, go into Idle state to pause
        if (!agent.pathPending && agent.remainingDistance < 0.5f) 
        {
            ChangeState(EnemyState.Idle);
        }
    }

    protected void GetNewPatrolTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        
        // Find the closest valid point on the NavMesh
        if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, 1)) 
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
            isMovingToPatrolPoint = true;
        }
    }

    protected abstract void AttackBehavior(float distanceToPlayer);

    protected void MoveTowards(Vector3 target, float moveSpeed)
    {
        agent.speed = moveSpeed;
        agent.SetDestination(target);
    }

    protected void MoveAwayFrom(Vector3 target, float moveSpeed)
    {
        Vector3 direction = (transform.position - target).normalized;
        Vector3 fleePosition = transform.position + direction * 5f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(fleePosition, out hit, 5f, NavMesh.AllAreas))
        {
            agent.speed = moveSpeed;
            agent.SetDestination(hit.position);
        }
    }

    public void FlashColor(Color color, float duration = 0.1f)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
            CancelInvoke("ResetColor");
            Invoke("ResetColor", duration);
        }
    }

    private void ResetColor()
    {
        if (meshRenderer != null) meshRenderer.material.color = originalColor;
    }
}
