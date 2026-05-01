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
    public Renderer bodyRenderer; // Changed from MeshRenderer to Renderer to support SkinnedMeshRenderer
    public Health playerHealth; 
    public AudioSource audioSource;
    public Animator animator;

    // --- ANIMATOR HASHES ---
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    private static readonly int Attack3Hash = Animator.StringToHash("Attack3");
    private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");

    [Header("Input Actions")] public InputActionReference moveAction;
    public InputActionReference dashAction;
    public InputActionReference attackAction;
    public InputActionReference specialAttackAction;

    [Header("Identification")] public LayerMask enemyLayer;

    [Header("Movement Stats")] public float speed = 14f;
    public float dashSpeed = 30f;
    public float gravity = 25f;
    [SerializeField] private float runAnimationSpeed = 1.5f;

    [Header("Dash")] public float dashDuration = 0.2f;
    private float dashTimer = 0;
    private TrailRenderer dashTrail;

    [Header("Combat Stats")] public float weaponBaseDamage = 10f;
    public float baseCritChance = 0.05f;
    public bool wasHit = false;
    public float attackRange = 2.0f;
    public float specialRange = 5.0f;
    [SerializeField] private float knockBackForce = 7;

    [Header("Combo Settings")] 
    public int comboStep = 0;
    public float comboResetTime = 1.0f;
    private float lastAttackTime = 0;
    [SerializeField] private float attackAnimationSpeed = 1.5f;
    [SerializeField] private float specialAttackAnimationSpeed = 1.2f;

    [Header("Combo Timing")]
    private bool isComboWindowOpen = false;
    private float fallbackTimer = 0f;

    [Header("Scissor / Hitbox Objects")]
    [SerializeField] private GameObject generalAttackHitbox; 
    [SerializeField] private GameObject Hitbox_attack12;
    [SerializeField] private GameObject Hitbox_attack3;
    [SerializeField] private Transform modelTransform;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip singleScissorClip;
    [SerializeField] private AudioClip doubleScissorClip;

    private Quaternion originalRotation;

    private void Start()
    {
        if (modelTransform == null) modelTransform = transform;
        originalRotation = modelTransform.localRotation;
        
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Setup hitbox component if missing
        SetupScissorTrigger(generalAttackHitbox);
        SetupScissorTrigger(Hitbox_attack12);
        SetupScissorTrigger(Hitbox_attack3);

        // Ensure hitboxes are off at start
        if (generalAttackHitbox) generalAttackHitbox.SetActive(false);
        if (Hitbox_attack12) Hitbox_attack12.SetActive(false);
        if (Hitbox_attack3) Hitbox_attack3.SetActive(false);

        if (bodyRenderer != null) originalColor = bodyRenderer.material.color;
    }

    private void SetupScissorTrigger(GameObject scissor)
    {
        if (scissor == null) return;
        if (scissor.GetComponent<Scissors>() == null)
            scissor.AddComponent<Scissors>();
        
        // Ensure there is a trigger collider
        Collider col = scissor.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    [Header("Special Attack")] public float specialCooldown = 10f;
    public float specialTimer = 0; // Made public for UI access

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 dashDirection = Vector3.zero;
    private float verticalVelocity = 0f;
    private Color originalColor;
    
    public static bool IsPaused = false;

    // --- 2. SETUP INPUTS ---

    private void OnEnable()
    {
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (dashTrail == null) dashTrail = GetComponent<TrailRenderer>();

        if (enemyLayer.value == 0)
            enemyLayer = 1 << LayerMask.NameToLayer("Enemy");

        if (dashTrail != null)
        {
            dashTrail.emitting = false;
            dashTrail.Clear();
        }

        moveAction.action.Enable();
        dashAction.action.Enable();
        attackAction.action.Enable();
        if (specialAttackAction != null) specialAttackAction.action.Enable();

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
        // The Health.cs now handles the Orange Hit Flash automatically
    }

    void Update()
    {
        if (specialTimer > 0) specialTimer -= Time.deltaTime;

        // FAILSAFE: If we are stuck in an attack state for too long, force return to idle
        if (currentState == PlayerState.Attacking || currentState == PlayerState.SpecialAttacking)
        {
            fallbackTimer += Time.deltaTime;
            if (fallbackTimer > 3.0f)
            {
                Debug.LogWarning($"[FAILSAFE] Stuck in {currentState} for 3.0s. Animator: {animator.GetCurrentAnimatorStateInfo(0).fullPathHash}. Transitioning: {animator.IsInTransition(0)}");
                ReturnToIdle();
            }
        }
        else
        {
            fallbackTimer = 0;
        }

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
        
        // LOCK MOVEMENT during attacks
        if (currentState != PlayerState.Attacking && currentState != PlayerState.SpecialAttacking)
        {
            if (currentState != PlayerState.Dashing) verticalVelocity -= gravity * Time.deltaTime;
            else verticalVelocity = 0;

            Vector3 finalMove = moveDirection;
            finalMove.y = verticalVelocity;
            controller.Move(finalMove * Time.deltaTime);
        }
        else
        {
            // Still apply gravity but no horizontal movement
            verticalVelocity -= gravity * Time.deltaTime;
            controller.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
        }
    }

    private void HandleIdleState()
    {
        CheckForCombatInputs();
        if (currentState != PlayerState.Idle) return;

        UpdateMovementAndRotation(); 
        if (moveDirection != Vector3.zero) SwitchState(PlayerState.Moving);
    }

    private void HandleMovingState()
    {
        CheckForCombatInputs();
        if (currentState != PlayerState.Moving) return;

        UpdateMovementAndRotation();
        if (moveDirection == Vector3.zero) SwitchState(PlayerState.Idle);
    }

    private void CheckForCombatInputs()
    {
        if (IsPaused) return;
        
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
        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = true;
        }

        if (animator != null) animator.SetBool(IsDashingHash, true);

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
            if (animator != null) animator.SetBool(IsDashingHash, false);
            SwitchState(PlayerState.Idle);
        }
    }

    // --- COMBAT LOGIC ---
    private void PerformNormalAttack()
    {
        // TIMING CHECK: If we are already attacking, only allow next hit if window is open
        if (currentState == PlayerState.Attacking && !isComboWindowOpen)
        {
            return; // Ignore mash
        }

        if (wasHit)
        {
            ResetCombo();
            wasHit = false;
        }

        // CRITICAL: Clear ALL pending triggers to prevent "phantom" attacks later
        if (animator != null)
        {
            animator.ResetTrigger(Attack1Hash);
            animator.ResetTrigger(Attack2Hash);
            animator.ResetTrigger(Attack3Hash);
            animator.ResetTrigger(HeavyAttackHash);
        }

        lastAttackTime = Time.time;
        comboStep++;
        isComboWindowOpen = false; // Close window as hit is accepted
        fallbackTimer = 0; // Reset failsafe on new input

        float currentDamage = weaponBaseDamage;
        float currentCritChance = baseCritChance;
        Color comboColor = Color.white;
        AudioClip clipToPlay = singleScissorClip;
        string targetState = "Attack_01";

        switch (comboStep)
        {
            case 1:
                comboColor = Color.white;
                clipToPlay = singleScissorClip;
                targetState = "Attack_01";
                break;
            case 2:
                currentDamage *= 1.2f; // Slightly more damage
                comboColor = Color.yellow;
                clipToPlay = singleScissorClip;
                targetState = "Attack_02";
                break;
            case 3:
                currentDamage *= 1.5f; // Stronger finisher
                currentCritChance += 0.25f;
                comboColor = Color.red;
                clipToPlay = doubleScissorClip;
                targetState = "Attack_03";
                break;
            default:
                ResetCombo();
                comboStep = 1;
                clipToPlay = singleScissorClip;
                targetState = "Attack_01";
                break;
        }

        // --- THE "SECRET SAUCE" FOR RESPONSIVE COMBAT ---
        if (animator != null)
        {
            animator.CrossFadeInFixedTime(targetState, 0.05f);
            Debug.Log($"[COMBO] Playing {targetState} (Step {comboStep})");
        }

        GameObject activeHitbox = (comboStep == 3) ? Hitbox_attack3 : Hitbox_attack12;
        if (activeHitbox == null) activeHitbox = generalAttackHitbox; 

        if (activeHitbox != null)
        {
            Scissors s = activeHitbox.GetComponent<Scissors>();
            if (s != null)
            {
                // Incrementing knockback: 3, 5, 8
                float currentKnockback = 3f;
                if (comboStep == 2) currentKnockback = 5f;
                else if (comboStep == 3) currentKnockback = 8f;
                
                s.Initialize(currentDamage, currentCritChance, currentKnockback);
            }
        }

        if (audioSource != null && clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }

        SetPlayerColor(comboColor);
        SwitchState(PlayerState.Attacking);
    }

    // --- ANIMATION EVENTS ---

    public void OpenComboWindow() => isComboWindowOpen = true;
    public void CloseComboWindow() => isComboWindowOpen = false;

    // Called by Animation Events in the FBX animations
    public void EnableHitbox()
    {
        // Ignore events if they fire when we aren't in a combo step (prevents phantom damage)
        if (comboStep == 0) return;

        Debug.Log($"Hitbox ENABLED via Animation Event. Combo Step: {comboStep}");
        
        if (comboStep == 3)
        {
            EnableHitbox3();
        }
        else
        {
            EnableHitbox12();
        }
    }

    // Specific methods for cleaner Animation Events
    public void EnableHitbox12()
    {
        // EXCLUSIVE: Turn off others first
        if (Hitbox_attack3) Hitbox_attack3.SetActive(false);
        if (generalAttackHitbox) generalAttackHitbox.SetActive(false);

        if (Hitbox_attack12) 
        {
            Hitbox_attack12.SetActive(true);
            Debug.Log("Activated Hitbox_attack12 specifically");
        }
        else if (generalAttackHitbox) generalAttackHitbox.SetActive(true);
    }

    public void EnableHitbox3()
    {
        // EXCLUSIVE: Turn off others first
        if (Hitbox_attack12) Hitbox_attack12.SetActive(false);
        if (generalAttackHitbox) generalAttackHitbox.SetActive(false);

        if (Hitbox_attack3) 
        {
            Debug.Log($"[HITBOX DEBUG] Attempting to activate Hitbox_attack3. Current State: {Hitbox_attack3.activeSelf}");
            Hitbox_attack3.SetActive(true);
            Debug.Log($"[HITBOX DEBUG] Hitbox_attack3 is now: {Hitbox_attack3.activeInHierarchy}");
        }
        else 
        {
            Debug.LogError("[HITBOX DEBUG] Hitbox_attack3 is NULL! Please check the Inspector.");
            if (generalAttackHitbox) generalAttackHitbox.SetActive(true);
        }
    }

    public void DisableHitbox()
    {
        Debug.Log("Hitboxes DISABLED");
        if (generalAttackHitbox) generalAttackHitbox.SetActive(false);
        if (Hitbox_attack12) Hitbox_attack12.SetActive(false);
        if (Hitbox_attack3) Hitbox_attack3.SetActive(false);
    }

    public void ReturnToIdle()
    {
        // Only return to idle if enough time has passed since the LAST attack trigger
        // AND we aren't already in the middle of a CrossFade/Trigger for the next step
        if (Time.time - lastAttackTime > 0.15f)
        {
            ResetCombo();
            SwitchState(PlayerState.Idle);
        }
    }

    [ContextMenu("Debug: Play Attack 3")]
    public void DebugPlayAttack3()
    {
        comboStep = 3;
        if (animator != null) animator.SetTrigger(Attack3Hash);
        SwitchState(PlayerState.Attacking);
    }

    private void UpdateMovementAndRotation()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        if (input != Vector2.zero)
        {
            moveDirection = new Vector3(input.x, 0, input.y).normalized * speed;
            transform.forward = new Vector3(moveDirection.x, 0, moveDirection.z);
        }
        else
            moveDirection = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(IsMovingHash, moveDirection != Vector3.zero);
        }
    }

    private void ResetCombo()
    {
        comboStep = 0;
        isComboWindowOpen = false;
        SetPlayerColor(originalColor);
        if (animator != null) animator.ResetTrigger(HeavyAttackHash);
    }

    public void OnPlayerHit()
    {
        wasHit = true;
        ResetCombo();
        // The orange flash is now handled by Health.cs
    }

    private void SetPlayerColor(Color color)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = color;
        }
    }

    private void HandleAttackingState()
    {
        CheckForCombatInputs(); // Allow chaining attacks
    }

    private void PerformSpecialAttack()
    {
        specialTimer = specialCooldown;
        lastAttackTime = Time.time;
        fallbackTimer = 0;
        SetPlayerColor(Color.cyan);
        
        if (animator != null)
        {
            // CRITICAL: Clear ALL attack triggers to avoid accidental follow-ups or double fires
            animator.ResetTrigger(Attack1Hash);
            animator.ResetTrigger(Attack2Hash);
            animator.ResetTrigger(Attack3Hash);
            animator.ResetTrigger(HeavyAttackHash);

            // Use CrossFade for immediate transition (matches normal attack logic)
            animator.CrossFadeInFixedTime("HeavyAttack", 0.05f);
        }
        SwitchState(PlayerState.SpecialAttacking);
    }

    // Called by Animation Event during the Heavy Attack animation
    public void ExecuteSpecialAttackDamage()
    {
        Debug.Log("Executing Special Attack (Sphere AOE Only)");
        
        // 1. Damage + Knockback (Sphere)
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, specialRange, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            Health h = enemy.GetComponent<Health>();
            if (h != null) h.TakeDamage(weaponBaseDamage * 2, transform.position, 10f);
        }
        
        // Visual debug for AOE in scene
        StartCoroutine(ShowSpecialAOEVisual());
    }

    private IEnumerator ShowSpecialAOEVisual()
    {
        showSpecialGizmo = true;
        yield return new WaitForSeconds(0.3f);
        showSpecialGizmo = false;
    }

    private bool showSpecialGizmo = false;

    private void OnDrawGizmos()
    {
        if (showSpecialGizmo)
        {
            Gizmos.color = new Color(0, 1, 1, 0.4f);
            Gizmos.DrawSphere(transform.position, specialRange);
        }
        
        // Permanent Range Gizmos
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + transform.forward, specialRange);
    }

    private void HandleSpecialAttackState()
    {
        moveDirection = Vector3.zero;
        // Logic handled by Animation Events (ReturnToIdle)
    }

    private void SwitchState(PlayerState newState)
    {
        if (newState == PlayerState.Dashing)
        {
            if (playerHealth != null) playerHealth.isInvulnerable = true;
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex != -1) Physics.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, true);
        }
        else if (currentState == PlayerState.Dashing)
        {
            if (playerHealth != null) playerHealth.isInvulnerable = false;
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex != -1) Physics.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, false);
        }

        // Adjust animator speed based on state
        if (animator != null)
        {
            if (newState == PlayerState.Moving) animator.speed = runAnimationSpeed;
            else if (newState == PlayerState.Attacking) animator.speed = attackAnimationSpeed;
            else if (newState == PlayerState.SpecialAttacking) animator.speed = specialAttackAnimationSpeed;
            else animator.speed = 1.0f;
        }

        currentState = newState;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + transform.forward, specialRange);
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        verticalVelocity = force * 1.5f;
        Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z).normalized;
        StartCoroutine(KnockbackCoroutine(horizontalDir, force, duration));
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration)
    {
        float t = 0f;
        force = knockBackForce;
        while (t < duration)
        {
            controller.Move(direction * force * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
    }
}