using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerFSM : MonoBehaviour
{
    // --- 1. STATE DEFINITIONS ---
    public enum PlayerState
    {
        Idle,
        Moving,
        Dashing,
        Attacking,
        SpecialAttacking
    }

    [Header("State Tracker")] public PlayerState currentState = PlayerState.Idle;

    [Header("Components")] public CharacterController controller;
    public MeshRenderer bodyRenderer;
    public Health playerHealth; // Link the Health script here

    [Header("Input Actions")] public InputActionReference moveAction;
    public InputActionReference dashAction;
    public InputActionReference attackAction;
    public InputActionReference specialAttackAction;

    [Header("Identification")] public LayerMask enemyLayer;

    [Header("Movement Stats")] public float speed = 14f;
    public float dashSpeed = 30f;
    public float gravity = 25f;

    [Header("Dash")] public float dashDuration = 0.2f;
    private float dashTimer = 0;
    private TrailRenderer dashTrail;

    [Header("Combat Stats")] public float weaponBaseDamage = 10f;
    public float baseCritChance = 0.05f;
    public bool wasHit = false;
    public float attackRange = 2.0f;
    public float specialRange = 5.0f;

    [Header("Combo Settings")] public int comboStep = 0;
    public float comboResetTime = 1.0f;
    private float lastAttackTime = 0;

    [Header("Special Attack")] public float specialCooldown = 10f;
    private float specialTimer = 0;

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 dashDirection = Vector3.zero;
    private float verticalVelocity = 0f;
    private Color originalColor;
    private float visualFlashTimer = 0;

    // --- 2. SETUP INPUTS ---

    private void OnEnable()
    {
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (dashTrail == null) dashTrail = GetComponent<TrailRenderer>();
        if (enemyLayer.value == 0) enemyLayer = LayerMask.GetMask("Enemy");

        if (dashTrail != null)
        {
            dashTrail.emitting = false;
            dashTrail.Clear();
        }

        moveAction.action.Enable();
        dashAction.action.Enable();
        attackAction.action.Enable();
        if (specialAttackAction != null) specialAttackAction.action.Enable();

        if (bodyRenderer != null) originalColor = bodyRenderer.material.color;

        // Setup health event to trigger combo breaks
        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken.AddListener(OnPlayerDamage);
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null) moveAction.action.Disable();
        if (dashAction != null && dashAction.action != null) dashAction.action.Disable();
        if (attackAction != null && attackAction.action != null) attackAction.action.Disable();
        if (specialAttackAction != null && specialAttackAction.action != null) specialAttackAction.action.Disable();

        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken.RemoveListener(OnPlayerDamage);
        }
    }

    private void OnPlayerDamage(float damage)
    {
        wasHit = true;
        ResetCombo();
        FlashColor(Color.magenta);
    }

    void Update()
    {
        if (specialTimer > 0) specialTimer -= Time.deltaTime;
        if (visualFlashTimer > 0) visualFlashTimer -= Time.deltaTime;

        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
        {
            ResetCombo();
        }

        switch (currentState)
        {
            case PlayerState.Idle:
                HandleIdleState();
                break;
            case PlayerState.Moving:
                HandleMovingState();
                break;
            case PlayerState.Dashing:
                HandleDashingState();
                break;
            case PlayerState.Attacking:
                HandleAttackingState();
                break;
            case PlayerState.SpecialAttacking:
                HandleSpecialAttackState();
                break;
        }

        ApplyMovement();
    }

    private void ApplyMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        if (currentState != PlayerState.Dashing) verticalVelocity -= gravity * Time.deltaTime;
        else verticalVelocity = 0;

        Vector3 finalMove = moveDirection;
        finalMove.y = verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    private void HandleIdleState()
    {
        moveDirection = Vector3.zero;
        CheckForCombatInputs();
        if (currentState != PlayerState.Idle) return;

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        if (input != Vector2.zero) SwitchState(PlayerState.Moving);
    }

    private void HandleMovingState()
    {
        CheckForCombatInputs();
        if (currentState != PlayerState.Moving) return;

        Vector2 input = Vector2.zero;
        if (moveAction != null && moveAction.action != null)
        {
            input = moveAction.action.ReadValue<Vector2>();
        }

        if (input == Vector2.zero)
        {
            SwitchState(PlayerState.Idle);
            return;
        }

        moveDirection = new Vector3(input.x, 0, input.y).normalized * speed;
        if (moveDirection != Vector3.zero) transform.forward = new Vector3(moveDirection.x, 0, moveDirection.z);
    }

    private void CheckForCombatInputs()
    {
        if (dashAction.action.WasPressedThisFrame())
        {
            StartDash(moveDirection.normalized);
            return;
        }

        if (attackAction.action.WasPressedThisFrame())
        {
            PerformNormalAttack();
            return;
        }

        if (specialAttackAction != null && specialAttackAction.action.WasPressedThisFrame() && specialTimer <= 0)
        {
            PerformSpecialAttack();
            return;
        }
    }

    private void StartDash(Vector3 direction)
    {
        dashDirection = direction != Vector3.zero ? direction : transform.forward;
        dashTimer = dashDuration;
        dashDirection.y = 0;
        // FlashColor(Color.deepPink);
        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = true;
        }

        SwitchState(PlayerState.Dashing);
    }

    private void HandleDashingState()
    {
        moveDirection = dashDirection * dashSpeed;
        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0)
        {
            dashTrail.Clear();
            dashTrail.emitting = false;
            SwitchState(PlayerState.Idle);
        }
    }

    // --- COMBAT LOGIC ---
    private void PerformNormalAttack()
    {
        if (wasHit)
        {
            ResetCombo();
            wasHit = false;
        }

        lastAttackTime = Time.time;
        comboStep++;

        float currentDamage = weaponBaseDamage;
        float currentCritChance = baseCritChance;
        Color flashColor = Color.white;

        switch (comboStep)
        {
            case 1:
                flashColor = Color.white;
                break;
            case 2:
                currentDamage *= 1.1f;
                flashColor = Color.yellow;
                break;
            case 3:
                currentDamage *= 1.3f;
                currentCritChance += 0.20f;
                flashColor = Color.red;
                ResetCombo();
                break;
            default:
                ResetCombo();
                comboStep = 1;
                break;
        }

        FlashColor(flashColor);
        CheckHit(attackRange, currentDamage, currentCritChance, flashColor);
        SwitchState(PlayerState.Attacking);
    }

    private void CheckHit(float range, float damage, float crit, Color vfxColor)
    {
        Vector3 hitPosition = transform.position + transform.forward * 1.5f;

        GameObject vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vfx.transform.position = hitPosition;
        vfx.transform.localScale = Vector3.one * range;
        vfx.GetComponent<Collider>().enabled = false;

        // Simple color setup for built-in shader
        vfx.GetComponent<MeshRenderer>().material.color = new Color(vfxColor.r, vfxColor.g, vfxColor.b, 0.4f);

        Destroy(vfx, 0.1f);

        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward, range, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                bool isCrit = Random.value < crit;
                float finalDamage = isCrit ? damage * 2 : damage;
                enemyHealth.TakeDamage(finalDamage);
            }
        }
    }

    private void ResetCombo()
    {
        comboStep = 0;
    }

    public void OnPlayerHit()
    {
        // Still available for manual calls if needed
        wasHit = true;
        ResetCombo();
        FlashColor(Color.magenta);
        Debug.Log("<color=red>Player Hit! Combo Broken.</color>");
    }

    private void FlashColor(Color color)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = color;
            visualFlashTimer = 0.15f;
            Invoke("ResetColor", 0.15f);
        }
    }

    private void ResetColor()
    {
        if (bodyRenderer != null && visualFlashTimer <= 0) bodyRenderer.material.color = originalColor;
    }

    private void HandleAttackingState()
    {
        // moveDirection = Vector3.zero;    // the player can during attack
        if (Time.time - lastAttackTime > 0.3f) SwitchState(PlayerState.Idle);
    }

    private void PerformSpecialAttack()
    {
        specialTimer = specialCooldown;
        FlashColor(Color.cyan);
        CheckHit(specialRange, weaponBaseDamage * 2, 0.40f, Color.cyan);
        Debug.Log("SPECIAL ATTACK! AOE Pushback.");
        SwitchState(PlayerState.SpecialAttacking);
    }

    private void HandleSpecialAttackState()
    {
        moveDirection = Vector3.zero;
        if (Time.time - lastAttackTime > 0.5f) SwitchState(PlayerState.Idle);
    }

    private void SwitchState(PlayerState newState)
    {
        currentState = newState;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + transform.forward, specialRange);
    }

    private void OnGUI()
    {
        if (playerHealth == null) return;

        Vector2 pos = new Vector2(20, 20);
        Vector2 size = new Vector2(200, 20);

        GUI.Box(new Rect(pos.x, pos.y, size.x, size.y), "");
        GUI.color = Color.green;
        GUI.Box(new Rect(pos.x, pos.y, size.x * (playerHealth.currentHealth / playerHealth.maxHealth), size.y),
            "PLAYER HP: " + (int)playerHealth.currentHealth);
        GUI.color = Color.white;

        if (specialTimer > 0)
        {
            GUI.Label(new Rect(pos.x, pos.y + 30, 200, 20), "Special CD: " + specialTimer.ToString("F1") + "s");
        }
        else
        {
            GUI.Label(new Rect(pos.x, pos.y + 30, 200, 20), "SPECIAL READY (RMB)");
        }

        GUI.Label(new Rect(pos.x, pos.y + 50, 200, 20), "Combo Step: " + comboStep);

        // Add a clickable GUI button for quick testing
        if (GUI.Button(new Rect(pos.x, pos.y + 75, 150, 25), "Reset All Health"))
        {
            playerHealth.ResetHealth();
        }
    }

    // --KNOCKBACK TRAP EFFECT--
    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        verticalVelocity = force * 1.5f;
        Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z).normalized;
        StartCoroutine(KnockbackCoroutine(horizontalDir, force, duration));
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            controller.Move(direction * force * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
    }
}