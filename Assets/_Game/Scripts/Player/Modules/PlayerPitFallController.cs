using Core.Utils;
using UnityEngine;

/// <summary>
/// Tracks the player's last safe grounded position and handles falling into pits when dashing:
/// animates a short drop into the pit, teleports back to safety, applies trap-fair damage,
/// grants brief respawn invulnerability, and triggers the semi-transparent respawn blink.
/// </summary>
public class PlayerPitFallController : MonoBehaviour, ITeleportListener
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private readonly Collider[] overlapHits = new Collider[8];

    private Player player;
    private PlayerRespawnBlink respawnBlink;
    private Vector3 lastSafePosition;
    private bool hasSafePosition;
    private bool isFalling;
    private bool isInternalTeleport;
    private float fallElapsed;
    private float activeFallDuration;
    private Vector3 fallStartPosition;
    private Vector3 fallDropTarget;
    private Bounds activePitBounds;
    private float invulnerabilityTimer;

    public bool IsFalling => isFalling;
    public bool HasSafePosition => hasSafePosition;
    public Vector3 LastSafePosition => lastSafePosition;
    public float RemainingRespawnInvulnerability => invulnerabilityTimer;
    public PlayerRespawnBlink RespawnBlink => respawnBlink;

    private Health PlayerHealth => player != null ? player.playerHealth : null;
    private CharacterController Controller => player != null ? player.controller : null;
    private bool IsGrounded => Controller != null && Controller.enabled && Controller.isGrounded;

    private float PitFallDamage => player != null && player.Config != null
        ? player.Config.pitFallDamage
        : PitFallRules.DefaultDamage;

    private float FallDuration => player != null && player.Config != null
        ? player.Config.fallDuration
        : PitFallRules.DefaultFallDuration;

    private float FallDepth => player != null && player.Config != null
        ? player.Config.fallDepth
        : PitFallRules.DefaultFallDepth;

    private float RespawnClearance => player != null && player.Config != null
        ? player.Config.pitRespawnClearance
        : PitFallRules.DefaultRespawnClearance;

    private float RespawnBlinkDuration => player != null && player.Config != null
        ? player.Config.respawnBlinkDuration
        : PitFallRules.DefaultBlinkDuration;

    private float RespawnBlinkInterval => player != null && player.Config != null
        ? player.Config.respawnBlinkInterval
        : PitFallRules.DefaultBlinkInterval;

    private float RespawnMinAlpha => player != null && player.Config != null
        ? player.Config.respawnMinAlpha
        : PitFallRules.DefaultMinAlpha;

    private float RespawnInvulnerabilityDuration => player != null && player.Config != null
        ? player.Config.respawnInvulnerabilityDuration
        : PitFallRules.DefaultInvulnerabilityDuration;

    public void Initialize(Player owner)
    {
        player = owner;
        respawnBlink = gameObject.GetOrAddComponent<PlayerRespawnBlink>();
        respawnBlink.Initialize(owner);
        lastSafePosition = transform.position;
    }

    public void RecordSafePosition(Vector3 position)
    {
        lastSafePosition = position;
        hasSafePosition = true;
    }

    public void RecordSafePositionBeforeDash()
    {
        if (isFalling)
            return;

        if ((IsGrounded || !hasSafePosition) && !IsOverlappingAnyPitXZ(transform.position))
        {
            lastSafePosition = transform.position;
            hasSafePosition = true;
        }
    }

    public void OnTeleported()
    {
        if (isInternalTeleport)
            return;

        lastSafePosition = transform.position;
        hasSafePosition = false;
    }

    public void Tick()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (player == null)
            return;

        float dt = Mathf.Max(0f, deltaTime);

        if (isFalling)
        {
            fallElapsed += dt;
            if (fallElapsed >= activeFallDuration)
            {
                transform.position = fallDropTarget;
                CompleteRespawn();
            }
            else
            {
                float t = activeFallDuration > 0.0001f ? fallElapsed / activeFallDuration : 1f;
                transform.position = PitFallRules.EvaluateFallPosition(fallStartPosition, fallDropTarget, t);
            }

            return;
        }

        UpdateRespawnInvulnerability(dt);

        if (player.currentState == Player.PlayerState.Dashing)
        {
            DetectPitContactWhileDashing();
            return;
        }

        if (IsGrounded && !IsOverlappingAnyPitXZ(transform.position))
        {
            lastSafePosition = transform.position;
            hasSafePosition = true;
        }
    }

    public bool TryTriggerPitFall(IPit pit, Vector3 contactNormal)
    {
        if (pit == null || player == null || isFalling)
            return false;

        Vector3 dashDirection = player.Dash != null && player.Dash.CurrentDirection.sqrMagnitude > 0.0001f
            ? player.Dash.CurrentDirection
            : transform.forward;

        Bounds pitBounds = pit.WorldBounds;
        bool isDashing = player.currentState == Player.PlayerState.Dashing;

        if (!PitFallRules.ShouldTriggerFall(isDashing, isFalling, dashDirection, contactNormal, transform.position, pitBounds))
            return false;

        BeginFall(pitBounds, dashDirection);
        return true;
    }

    private void BeginFall(Bounds pitBounds, Vector3 dashDirection)
    {
        isFalling = true;
        fallElapsed = 0f;
        activeFallDuration = Mathf.Max(0.01f, FallDuration);
        activePitBounds = pitBounds;
        invulnerabilityTimer = 0f;
        respawnBlink?.StopBlink();

        player.InputLocked = true;
        player.SwitchState(Player.PlayerState.Idle);

        if (player.Locomotion != null)
        {
            player.Locomotion.StopAllCoroutines();
            player.Locomotion.ResetToIdle();
        }

        if (player.animator != null)
            player.animator.SetBool(IsMovingHash, false);

        if (PlayerHealth != null)
            PlayerHealth.isInvulnerable = true;

        if (Controller != null)
            Controller.enabled = false;

        fallStartPosition = transform.position;
        fallDropTarget = PitFallRules.ComputeFallDropTarget(fallStartPosition, dashDirection, pitBounds, FallDepth);
    }

    private void CompleteRespawn()
    {
        Vector3 baseSafe = hasSafePosition ? lastSafePosition : fallStartPosition;
        Vector3 respawnPosition = PitFallRules.ComputeSafeRespawnPosition(baseSafe, activePitBounds, RespawnClearance);

        TeleportToSafePosition(respawnPosition);
        lastSafePosition = respawnPosition;
        hasSafePosition = true;

        // Snapshot clean material state before Health's orange hit-flash mutates material color.
        respawnBlink?.CaptureIfNeeded();

        Health health = PlayerHealth;
        if (health != null)
        {
            health.isInvulnerable = false;
            float damage = PitFallDamage;
            if (damage > 0f)
                health.TakeDamage(damage);
        }

        isFalling = false;
        player.InputLocked = false;

        bool survived = health == null || health.currentHealth > 0f;
        if (!survived)
            return;

        float invulnDuration = RespawnInvulnerabilityDuration;
        if (invulnDuration > 0f && health != null)
        {
            invulnerabilityTimer = invulnDuration;
            health.isInvulnerable = true;
        }

        float blinkDuration = RespawnBlinkDuration;
        if (blinkDuration > 0f && respawnBlink != null)
        {
            respawnBlink.StartBlink(blinkDuration, RespawnBlinkInterval, RespawnMinAlpha);
        }
    }

    private void TeleportToSafePosition(Vector3 position)
    {
        CharacterController controller = Controller;
        if (controller != null)
            controller.enabled = false;

        transform.position = position;

        if (player != null && player.Locomotion != null)
            player.Locomotion.ResetToIdle();

        if (controller != null)
            controller.enabled = true;

        isInternalTeleport = true;
        try
        {
            foreach (ITeleportListener listener in GetComponentsInChildren<ITeleportListener>())
                listener.OnTeleported();

            foreach (Cloth cloth in GetComponentsInChildren<Cloth>())
            {
                if (cloth != null && cloth.enabled)
                    cloth.ClearTransformMotion();
            }
        }
        finally
        {
            isInternalTeleport = false;
        }
    }

    private void UpdateRespawnInvulnerability(float dt)
    {
        if (invulnerabilityTimer <= 0f)
            return;

        invulnerabilityTimer -= dt;
        if (invulnerabilityTimer > 0f)
        {
            if (PlayerHealth != null)
                PlayerHealth.isInvulnerable = true;
        }
        else
        {
            invulnerabilityTimer = 0f;
            if (PlayerHealth != null && player.currentState != Player.PlayerState.Dashing)
                PlayerHealth.isInvulnerable = false;
        }
    }

    private void DetectPitContactWhileDashing()
    {
        Vector3 probeCenter = Controller != null
            ? transform.TransformPoint(Controller.center)
            : transform.position + Vector3.up * 0.8f;

        float probeRadius = Controller != null
            ? Controller.radius + Controller.skinWidth + 0.05f
            : 0.6f;

        int count = Physics.OverlapSphereNonAlloc(probeCenter, probeRadius, overlapHits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider col = overlapHits[i];
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            IPit pit = col.GetComponentInParent<IPit>();
            if (pit != null && TryTriggerPitFall(pit, Vector3.zero))
                return;
        }
    }

    private bool IsOverlappingAnyPitXZ(Vector3 position)
    {
        int count = Physics.OverlapSphereNonAlloc(position + Vector3.up * 0.5f, 0.2f, overlapHits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Collider col = overlapHits[i];
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            IPit pit = col.GetComponentInParent<IPit>();
            if (pit != null && PitFallRules.IsInsidePitXZ(position, pit.WorldBounds))
                return true;
        }

        return false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isFalling || player == null || player.currentState != Player.PlayerState.Dashing)
            return;

        if (hit.collider == null)
            return;

        IPit pit = hit.collider.GetComponentInParent<IPit>();
        if (pit != null)
            TryTriggerPitFall(pit, hit.normal);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleTriggerPit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHandleTriggerPit(other);
    }

    private void TryHandleTriggerPit(Collider other)
    {
        if (isFalling || player == null || player.currentState != Player.PlayerState.Dashing)
            return;

        if (other == null || other.transform.IsChildOf(transform))
            return;

        IPit pit = other.GetComponentInParent<IPit>();
        if (pit != null)
            TryTriggerPitFall(pit, Vector3.zero);
    }

    private void OnDisable()
    {
        if (isFalling)
        {
            isFalling = false;
            if (player != null)
                player.InputLocked = false;

            if (Controller != null)
                Controller.enabled = true;
        }

        invulnerabilityTimer = 0f;
    }
}
