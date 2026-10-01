using UnityEngine;
using UnityEngine.AI;

public class CardChargeAttack : MonoBehaviour, IEnemyAttackStrategy, IEnemyAttackStateExitHandler
{
    private static readonly Color BiteColor = new Color(1f, 0.5f, 0f);

    [Header("Charge Settings")]
    [Min(0f)]
    [SerializeField] private float followDuration = 2f;
    [Min(0.01f)]
    [SerializeField] private float chargeSpeed = 8f;
    [Min(0f)]
    [SerializeField] private float chargeRange = 6f;
    [Min(0f)]
    [SerializeField] private float biteRange = 1.15f;
    [Min(0f)]
    [SerializeField] private float collisionRadius = 0.35f;

    [Header("Visuals")]
    [SerializeField] private Transform cardVisual;
    [SerializeField] private TextMesh rankAndSuitLabel;
    [Range(0f, 0.25f)]
    [SerializeField] private float dashPulseAmount = 0.08f;
    [Min(0f)]
    [SerializeField] private float dashPulseSpeed = 18f;

    private Vector3 chargeDirection;
    private Vector3 defaultScale;
    private EnemyVisualFeedback visualFeedback;
    private Color baseColor;
    private float chargeDistance;
    private bool chargeStarted;
    private bool biteStarted;
    private bool windupStarted;
    private bool executionComplete;

    public float ApproachDuration => followDuration;
    public float ExecutingDuration => chargeRange / Mathf.Max(chargeSpeed, 0.01f);
    public bool IsExecutionComplete => executionComplete;
    public bool BeginsWindupAtAnyDistance => true;

    private void Awake()
    {
        defaultScale = cardVisual.localScale;
        visualFeedback = GetComponent<EnemyVisualFeedback>();
        visualFeedback.Initialize(cardVisual.GetComponent<MeshRenderer>());
        baseColor = visualFeedback.BaseColor;
        SetRandomIdentity();
    }

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        owner.MoveTowards(owner.PlayerTransform.position, owner.Settings.speed);
        SetTelegraph(0f, baseColor);
    }

    public void OnWindup(Enemy owner)
    {
        if (!windupStarted)
        {
            windupStarted = true;
            executionComplete = false;
            chargeStarted = false;
            biteStarted = false;
            chargeDistance = 0f;
        }

        owner.Agent.ResetPath();
        Face(owner, owner.PlayerTransform.position);
        float distanceToPlayer = Vector3.Distance(owner.transform.position, owner.PlayerTransform.position);
        Color attackColor = distanceToPlayer <= biteRange
            ? BiteColor
            : Color.red;
        SetTelegraph(owner.AttackStateProgress, attackColor);
    }

    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        if (!chargeStarted)
        {
            chargeStarted = true;
            windupStarted = false;
            chargeDirection = owner.PlayerTransform.position - owner.transform.position;
            chargeDirection.y = 0f;
            chargeDirection = chargeDirection.sqrMagnitude > 0.001f
                ? chargeDirection.normalized
                : owner.transform.forward;
        }

        if (distanceToPlayer <= biteRange)
        {
            biteStarted = true;
            owner.Agent.ResetPath();
            owner.PlayerHealth?.TakeDamage(owner.Settings.damage, owner.transform.position);
            SetBiteVisual();
            executionComplete = true;
            return;
        }

        float step = Mathf.Min(chargeSpeed * Time.deltaTime, chargeRange - chargeDistance);
        if (IsBlocked(owner, step) || !IsNextPositionOnNavMesh(owner, step))
        {
            executionComplete = true;
            return;
        }

        owner.Agent.Move(chargeDirection * step);
        chargeDistance += step;
        SetDashVisual();
        executionComplete = chargeDistance >= chargeRange;
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
        cardVisual.localScale = defaultScale;
        visualFeedback.SetTelegraph(biteStarted ? BiteColor : baseColor);
    }

    public void OnAttackStateExit(Enemy owner, EnemyAttackStateMachine.State state)
    {
        if (state == EnemyAttackStateMachine.State.Windup)
            windupStarted = false;

        if (state == EnemyAttackStateMachine.State.Cooldown)
        {
            chargeDistance = 0f;
            chargeStarted = false;
            biteStarted = false;
            executionComplete = false;
            cardVisual.localScale = defaultScale;
            visualFeedback.ClearTelegraph();
        }
    }

    private void OnDisable()
    {
        if (cardVisual != null)
            cardVisual.localScale = defaultScale;

        if (visualFeedback != null)
            visualFeedback.ClearTelegraph();
    }

    private bool IsBlocked(Enemy owner, float step)
    {
        Vector3 origin = owner.transform.position + Vector3.up * 0.7f;
        RaycastHit[] hits = Physics.SphereCastAll(origin, collisionRadius, chargeDirection, step + collisionRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(owner.transform))
                continue;

            if (owner.PlayerTransform != null && hit.collider.transform.IsChildOf(owner.PlayerTransform))
                continue;

            return true;
        }

        return false;
    }

    private bool IsNextPositionOnNavMesh(Enemy owner, float step)
    {
        Vector3 nextPosition = owner.transform.position + chargeDirection * step;
        return NavMesh.SamplePosition(nextPosition, out _, 0.6f, NavMesh.AllAreas);
    }

    private static void Face(Enemy owner, Vector3 target)
    {
        Vector3 direction = target - owner.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            owner.transform.rotation = Quaternion.LookRotation(direction);
    }

    private void SetTelegraph(float amount, Color attackColor)
    {
        float foldedHeight = Mathf.Lerp(defaultScale.y, defaultScale.y * 0.5f, amount);
        cardVisual.localScale = new Vector3(defaultScale.x, foldedHeight, defaultScale.z);
        visualFeedback.SetTelegraph(Color.Lerp(baseColor, attackColor, amount));
    }

    private void SetDashVisual()
    {
        float pulse = Mathf.Sin(Time.time * dashPulseSpeed) * dashPulseAmount;
        cardVisual.localScale = new Vector3(
            defaultScale.x * (1f + pulse),
            defaultScale.y * (0.5f - pulse * 0.25f),
            defaultScale.z);
        visualFeedback.SetTelegraph(Color.red);
    }

    private void SetBiteVisual()
    {
        cardVisual.localScale = defaultScale;
        visualFeedback.SetTelegraph(BiteColor);
    }

    private void SetRandomIdentity()
    {
        string[] suits = { "♠", "♥", "♦", "♣" };
        int rank = Random.Range(1, 14);
        string rankText = rank switch { 1 => "A", 11 => "J", 12 => "Q", 13 => "K", _ => rank.ToString() };
        string suit = suits[Random.Range(0, suits.Length)];

        rankAndSuitLabel.text = rankText + suit;
        rankAndSuitLabel.color = suit == "♥" || suit == "♦" ? new Color(0.75f, 0.05f, 0.05f) : Color.black;
    }
}
