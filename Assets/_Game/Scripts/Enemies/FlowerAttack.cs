using UnityEngine;
using UnityEngine.AI;

public class FlowerAttack : MonoBehaviour, IEnemyAttackStrategy, IEnemyAttackStateExitHandler
{
    private static readonly Color BiteColor = new Color(1f, 0.5f, 0f);

    [Header("Burrow")]
    [Min(0f)]
    [SerializeField] private float burrowDelay = 1f;
    [Min(0.1f)]
    [SerializeField] private float undergroundTravelTime = 2f;
    [Min(0f)]
    [SerializeField] private float resurfaceOffset = 1.2f;
    [Min(0f)]
    [SerializeField] private float sinkDepth = 1.5f;

    [Header("Bite")]
    [Min(0f)]
    [SerializeField] private float biteRange = 1.5f;
    [Min(0f)]
    [SerializeField] private float biteDamage = 15f;

    [Header("Projectile")]
    [Min(0f)]
    [SerializeField] private float projectileRange = 6f;
    [Min(0f)]
    [SerializeField] private float projectileDamage = 10f;
    [Min(0.1f)]
    [SerializeField] private float projectileSpeed = 10f;
    [Min(0.05f)]
    [SerializeField] private float projectileSize = 0.35f;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private Material projectileMaterial;

    [Header("Timing")]
    [Min(0.05f)]
    [SerializeField] private float attackDuration = 0.4f;

    [Header("Visuals")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Material crackMaterial;
    [Min(0.1f)]
    [SerializeField] private float crackWidth = 0.5f;

    private EnemyVisualFeedback visualFeedback;
    private Health health;
    private NavMeshAgent agent;
    private Collider bodyCollider;
    private Transform crack;
    private Vector3 burrowOrigin;
    private Vector3 burrowDestination;
    private float approachTime;
    private bool burrowing;
    private bool windupStarted;
    private bool executeStarted;
    private bool useBite;

    public float ApproachDuration => burrowDelay + undergroundTravelTime;
    public float ExecutingDuration => attackDuration;
    public bool IsExecutionComplete => false;
    public bool BeginsWindupAtAnyDistance => true;

    private void Awake()
    {
        visualFeedback = GetComponent<EnemyVisualFeedback>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        bodyCollider = GetComponent<Collider>();
    }

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        approachTime += Time.deltaTime;
        Vector3 flatPlayer = Flat(owner.PlayerTransform.position);

        if (approachTime < burrowDelay)
        {
            Face(owner, flatPlayer);
            float t = burrowDelay > 0f ? approachTime / burrowDelay : 1f;
            SetSink(t);
            return;
        }

        if (!burrowing)
            StartBurrow(owner, flatPlayer);

        float travelT = Mathf.Clamp01((approachTime - burrowDelay) / undergroundTravelTime);
        Vector3 undergroundPosition = Vector3.Lerp(burrowOrigin, burrowDestination, travelT);
        owner.transform.position = undergroundPosition;
        UpdateCrack(burrowOrigin, undergroundPosition);
    }

    public void OnWindup(Enemy owner)
    {
        owner.Agent.ResetPath();
        Face(owner, owner.PlayerTransform.position);

        if (!windupStarted)
        {
            windupStarted = true;
            useBite = Vector3.Distance(owner.transform.position, owner.PlayerTransform.position) <= biteRange;
        }

        float progress = owner.AttackStateProgress;
        SetSink(1f - progress);
        visualFeedback.SetTelegraph(Color.Lerp(visualFeedback.BaseColor, useBite ? BiteColor : Color.red, progress));
    }

    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        if (executeStarted)
            return;

        executeStarted = true;
        SetSink(0f);

        if (useBite)
        {
            if (distanceToPlayer <= biteRange)
                owner.PlayerHealth?.TakeDamage(biteDamage, owner.transform.position);
            visualFeedback.SetTelegraph(BiteColor);
        }
        else
        {
            Shoot(owner);
            visualFeedback.SetTelegraph(Color.red);
        }
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
        visualFeedback.ClearTelegraph();
    }

    public void OnAttackStateExit(Enemy owner, EnemyAttackStateMachine.State state)
    {
        switch (state)
        {
            case EnemyAttackStateMachine.State.Approaching:
                ResetBurrow();
                break;
            case EnemyAttackStateMachine.State.Windup:
                windupStarted = false;
                SetSink(0f);
                break;
            case EnemyAttackStateMachine.State.Executing:
                executeStarted = false;
                break;
        }
    }

    private void OnDisable()
    {
        ResetBurrow();
        if (visualFeedback != null)
            visualFeedback.ClearTelegraph();
    }

    private void StartBurrow(Enemy owner, Vector3 lockedPlayerPosition)
    {
        burrowing = true;
        burrowOrigin = owner.transform.position;
        owner.Agent.ResetPath();
        burrowDestination = FindResurfacePoint(owner, lockedPlayerPosition);
        health.isInvulnerable = true;
        visualRoot.gameObject.SetActive(false);

        if (agent != null)
            agent.enabled = false;

        if (bodyCollider != null)
            bodyCollider.enabled = false;
    }

    private Vector3 FindResurfacePoint(Enemy owner, Vector3 lockedPlayerPosition)
    {
        const int steps = 8;
        float startAngle = Random.value * 360f;

        foreach (float radius in new[] { resurfaceOffset, resurfaceOffset * 1.5f })
        {
            for (int i = 0; i < steps; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, startAngle + i * 360f / steps, 0f) * Vector3.forward;
                Vector3 candidate = lockedPlayerPosition + direction * radius;
                candidate.y = owner.transform.position.y;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.5f, NavMesh.AllAreas) && IsClear(owner, hit.position))
                    return hit.position;
            }
        }

        return burrowOrigin;
    }

    private bool IsClear(Enemy owner, Vector3 position)
    {
        float radius = agent != null ? agent.radius * 0.8f : 0.4f;
        foreach (Collider other in Physics.OverlapSphere(position + Vector3.up * 0.6f, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (other.transform.IsChildOf(owner.transform))
                continue;

            if (owner.PlayerTransform != null && other.transform.IsChildOf(owner.PlayerTransform))
                continue;

            if (other.bounds.max.y <= position.y + 0.15f)
                continue;

            return false;
        }

        return true;
    }

    private void ResetBurrow()
    {
        approachTime = 0f;
        burrowing = false;

        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
        }

        if (health != null)
            health.isInvulnerable = false;

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(true);
            SetSink(0f);
        }

        if (crack != null)
            Destroy(crack.gameObject);
    }

    private void Shoot(Enemy owner)
    {
        Vector3 origin = shootPoint != null ? shootPoint.position : owner.transform.position + Vector3.up * 1.2f;
        Vector3 direction = Flat(owner.PlayerTransform.position) - Flat(owner.transform.position);
        if (direction.sqrMagnitude < 0.001f)
            direction = owner.transform.forward;

        EnemyStraightProjectile.Launch(origin, direction, owner, projectileDamage, projectileSpeed,
            projectileRange, projectileSize, projectileMaterial);
    }

    private void UpdateCrack(Vector3 from, Vector3 to)
    {
        from = Flat(from, transform.position.y + 0.03f);
        to = Flat(to, from.y);
        Vector3 span = to - from;
        if (span.sqrMagnitude < 0.0001f)
            return;

        if (crack == null)
        {
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "Flower Crack";
            Destroy(strip.GetComponent<Collider>());
            if (crackMaterial != null) strip.GetComponent<Renderer>().sharedMaterial = crackMaterial;
            crack = strip.transform;
        }

        crack.position = from + span * 0.5f;
        crack.rotation = Quaternion.LookRotation(span);
        crack.localScale = new Vector3(crackWidth, 0.02f, span.magnitude);
    }

    private void SetSink(float amount)
    {
        visualRoot.localPosition = Vector3.down * (sinkDepth * Mathf.Clamp01(amount));
    }

    private static Vector3 Flat(Vector3 position, float y = 0f) => new Vector3(position.x, y, position.z);

    private static void Face(Enemy owner, Vector3 target)
    {
        Vector3 direction = target - owner.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            owner.transform.rotation = Quaternion.LookRotation(direction);
    }
}
