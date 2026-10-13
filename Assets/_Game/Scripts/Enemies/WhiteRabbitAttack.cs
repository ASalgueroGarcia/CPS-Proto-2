using UnityEngine;
using UnityEngine.AI;

public class WhiteRabbitAttack : MonoBehaviour, IEnemyAttackStrategy, IEnemyAttackStateExitHandler
{
    private static readonly Color BombColor = new Color(1f, 0.5f, 0f);

    [Header("Kiting")]
    [Min(0f)]
    [SerializeField] private float idealDistance = 8f;
    [Min(0f)]
    [SerializeField] private float distanceDeadzone = 1.5f;
    [Min(0f)]
    [SerializeField] private float kiteDuration = 1.5f;

    [Header("Attack Choice")]
    [Range(0f, 1f)]
    [SerializeField] private float clockBombChance = 0.5f;

    [Header("Clock Bomb")]
    [Min(0f)]
    [SerializeField] private float bombDamage = 20f;
    [Min(0.1f)]
    [SerializeField] private float bombFuseTime = 2f;
    [Min(0.1f)]
    [SerializeField] private float bombRadius = 3f;
    [Min(0f)]
    [SerializeField] private float bombMaxThrowRange = 12f;
    [Min(0.05f)]
    [SerializeField] private float bombFlightTime = 0.8f;
    [Min(0f)]
    [SerializeField] private float bombArcHeight = 3f;
    [Min(0f)]
    [SerializeField] private float bombKnockback = 6f;
    [Min(0.05f)]
    [SerializeField] private float bombScale = 0.4f;
    [SerializeField] private Transform bombThrowPoint;
    [SerializeField] private Material bombMaterial;
    [SerializeField] private Material bombIndicatorMaterial;
    [SerializeField] private Color bombBeepColor = Color.red;

    [Header("Waist Clock")]
    [Min(0f)]
    [SerializeField] private float clockDamage = 10f;
    [Min(0.1f)]
    [SerializeField] private float clockThrowSpeed = 14f;
    [Min(0.1f)]
    [SerializeField] private float clockReturnSpeed = 18f;
    [Min(0.1f)]
    [SerializeField] private float clockRange = 9f;
    [Min(0.05f)]
    [SerializeField] private float clockHeadRadius = 0.45f;
    [SerializeField] private Transform clockHead;
    [SerializeField] private Transform chain;

    private EnemyVisualFeedback visualFeedback;
    private Vector3 clockHomeLocal;
    private Quaternion clockHomeRotation;
    private Vector3 clockDirection;
    private float clockDistance;
    private bool clockReturning;
    private bool clockHit;
    private bool useBomb;
    private bool windupStarted;
    private bool executeStarted;
    private bool executionComplete;

    public float ApproachDuration => kiteDuration;
    public float ExecutingDuration => clockRange / Mathf.Max(clockThrowSpeed, 0.1f) + clockRange / Mathf.Max(clockReturnSpeed, 0.1f) + 0.25f;
    public bool IsExecutionComplete => executionComplete;
    public bool BeginsWindupAtAnyDistance => false;

    private void Awake()
    {
        visualFeedback = GetComponent<EnemyVisualFeedback>();
        clockHomeLocal = clockHead.localPosition;
        clockHomeRotation = clockHead.localRotation;
        chain.gameObject.SetActive(false);
    }

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        if (distanceToPlayer < idealDistance - distanceDeadzone)
            owner.MoveAwayFrom(owner.PlayerTransform.position, owner.Settings.speed);
        else if (distanceToPlayer > idealDistance + distanceDeadzone)
            owner.MoveTowards(owner.PlayerTransform.position, owner.Settings.speed);
        else
        {
            owner.Agent.ResetPath();
            Face(owner, owner.PlayerTransform.position);
        }
    }

    public void OnWindup(Enemy owner)
    {
        if (!windupStarted)
        {
            windupStarted = true;
            useBomb = Random.value < clockBombChance;
        }

        owner.Agent.ResetPath();
        Face(owner, owner.PlayerTransform.position);
        visualFeedback.SetTelegraph(Color.Lerp(visualFeedback.BaseColor, useBomb ? BombColor : Color.red, owner.AttackStateProgress));
    }

    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        if (!executeStarted)
        {
            executeStarted = true;
            if (useBomb)
            {
                ThrowBomb(owner);
                executionComplete = true;
                return;
            }

            StartClock(owner);
        }

        if (!useBomb)
            TickClock(owner);
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
        visualFeedback.ClearTelegraph();
        if (distanceToPlayer < idealDistance - distanceDeadzone)
            owner.MoveAwayFrom(owner.PlayerTransform.position, owner.Settings.speed);
    }

    public void OnAttackStateExit(Enemy owner, EnemyAttackStateMachine.State state)
    {
        if (state == EnemyAttackStateMachine.State.Windup)
            windupStarted = false;

        if (state == EnemyAttackStateMachine.State.Executing)
        {
            executeStarted = false;
            executionComplete = false;
            ResetClock();
        }
    }

    private void OnDisable()
    {
        ResetClock();
        if (visualFeedback != null)
            visualFeedback.ClearTelegraph();
    }

    private void ThrowBomb(Enemy owner)
    {
        Vector3 origin = bombThrowPoint != null ? bombThrowPoint.position : owner.transform.position + Vector3.up * 1.5f;
        Vector3 offset = owner.PlayerTransform.position - owner.transform.position;
        offset.y = 0f;
        Vector3 target = owner.transform.position + Vector3.ClampMagnitude(offset, bombMaxThrowRange);
        target.y = owner.PlayerTransform.position.y;
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            target = hit.position;

        ClockBomb.Launch(origin, target, owner, bombDamage, bombFuseTime, bombRadius, bombFlightTime,
            bombArcHeight, bombKnockback, bombScale, bombMaterial, bombIndicatorMaterial, bombBeepColor);
    }

    private void StartClock(Enemy owner)
    {
        Vector3 toPlayer = owner.PlayerTransform.position - owner.transform.position;
        toPlayer.y = 0f;
        clockDirection = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : owner.transform.forward;
        clockDistance = 0f;
        clockReturning = false;
        clockHit = false;
        chain.gameObject.SetActive(true);
    }

    private void TickClock(Enemy owner)
    {
        Vector3 home = owner.transform.TransformPoint(clockHomeLocal);

        if (!clockReturning)
        {
            float step = Mathf.Min(clockThrowSpeed * Time.deltaTime, clockRange - clockDistance);
            if (IsBlocked(owner, step))
                clockReturning = true;
            else
            {
                clockHead.position += clockDirection * step;
                clockDistance += step;
                clockReturning = clockDistance >= clockRange;
            }
        }
        else
        {
            clockHead.position = Vector3.MoveTowards(clockHead.position, home, clockReturnSpeed * Time.deltaTime);
            if ((clockHead.position - home).sqrMagnitude < 0.01f)
            {
                ResetClock();
                executionComplete = true;
                return;
            }
        }

        if (!clockHit)
            clockHit = TryHitPlayer(owner);

        UpdateChain(home);
    }

    private bool TryHitPlayer(Enemy owner)
    {
        foreach (Collider hit in Physics.OverlapSphere(clockHead.position, clockHeadRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (owner.PlayerTransform == null || !hit.transform.IsChildOf(owner.PlayerTransform))
                continue;

            owner.PlayerHealth?.TakeDamage(clockDamage, clockHead.position);
            return true;
        }

        return false;
    }

    private bool IsBlocked(Enemy owner, float step)
    {
        foreach (RaycastHit hit in Physics.SphereCastAll(clockHead.position, clockHeadRadius, clockDirection, step, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(owner.transform))
                continue;

            if (owner.PlayerTransform != null && hit.collider.transform.IsChildOf(owner.PlayerTransform))
                continue;

            return true;
        }

        return false;
    }

    private void UpdateChain(Vector3 home)
    {
        Vector3 span = clockHead.position - home;
        float length = span.magnitude;
        if (length < 0.01f)
            return;

        chain.position = home + span * 0.5f;
        chain.rotation = Quaternion.LookRotation(span);
        Vector3 scale = chain.localScale;
        chain.localScale = new Vector3(scale.x, scale.y, length / Mathf.Max(transform.lossyScale.z, 0.0001f));
    }

    private void ResetClock()
    {
        if (clockHead == null)
            return;

        clockHead.localPosition = clockHomeLocal;
        clockHead.localRotation = clockHomeRotation;
        chain.gameObject.SetActive(false);
    }

    private static void Face(Enemy owner, Vector3 target)
    {
        Vector3 direction = target - owner.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            owner.transform.rotation = Quaternion.LookRotation(direction);
    }
}
