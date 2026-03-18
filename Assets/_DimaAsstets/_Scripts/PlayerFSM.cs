using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("State Tracker")]
    public PlayerState currentState = PlayerState.Idle;

    [Header("Components")]
    public CharacterController controller;
    public MeshRenderer bodyRenderer; 

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference dashAction;
    public InputActionReference attackAction;
    public InputActionReference specialAttackAction;

    [Header("Identification")]
    public LayerMask enemyLayer; // The "Magic Number" mask for target layers

    [Header("Movement Stats")]
    public float speed = 8f;
    public float dashSpeed = 30f;
    public float gravity = 25f;
    
    [Header("Dash")]
    public float dashDuration = 0.2f;
    private float dashTimer = 0;

    [Header("Combat Stats")]
    public float weaponBaseDamage = 10f; 
    public float baseCritChance = 0.05f;
    public bool wasHit = false; 
    public float attackRange = 2.0f;
    public float specialRange = 5.0f;

    [Header("Combo Settings")]
    public int comboStep = 0; 
    public float comboResetTime = 1.0f;
    private float lastAttackTime = 0;

    [Header("Special Attack")]
    public float specialCooldown = 10f;
    private float specialTimer = 0;

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 dashDirection = Vector3.zero;
    private float verticalVelocity = 0f;
    private Color originalColor;

    // --- 2. SETUP INPUTS ---
    
    private void OnEnable()
    {
        moveAction.action.Enable();
        dashAction.action.Enable();
        attackAction.action.Enable();
        if (specialAttackAction != null) specialAttackAction.action.Enable();
        
        if (bodyRenderer != null) originalColor = bodyRenderer.material.color;
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        dashAction.action.Disable();
        attackAction.action.Disable();
        if (specialAttackAction != null) specialAttackAction.action.Disable();
    }

    void Update()
    {
        if (specialTimer > 0) specialTimer -= Time.deltaTime;

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

        Vector2 input = moveAction.action.ReadValue<Vector2>();
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
        SwitchState(PlayerState.Dashing);
    }

    private void HandleDashingState()
    {
        moveDirection = dashDirection * dashSpeed;
        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0) SwitchState(PlayerState.Idle);
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
        CheckHit(attackRange, currentDamage, currentCritChance);
        SwitchState(PlayerState.Attacking);
    }

    private void CheckHit(float range, float damage, float crit)
    {
        // PERFORMANCE: Physics now only checks the layers included in 'enemyLayer'
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward, range, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            CombatDummy dummy = enemy.GetComponent<CombatDummy>();
            if (dummy != null)
            {
                dummy.TakeDamage(damage, crit);
            }
        }
    }

    private void ResetCombo()
    {
        comboStep = 0;
    }

    public void OnPlayerHit()
    {
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
            Invoke("ResetColor", 0.15f);
        }
    }

    private void ResetColor()
    {
        if (bodyRenderer != null) bodyRenderer.material.color = originalColor;
    }

    private void HandleAttackingState()
    {
        moveDirection = Vector3.zero;
        if (Time.time - lastAttackTime > 0.3f) SwitchState(PlayerState.Idle);
    }

    private void PerformSpecialAttack()
    {
        specialTimer = specialCooldown;
        FlashColor(Color.cyan);
        CheckHit(specialRange, weaponBaseDamage * 2, 0.40f);
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
}