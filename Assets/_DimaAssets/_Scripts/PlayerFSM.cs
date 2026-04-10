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
    public Health playerHealth; 
    public AudioSource audioSource;

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
    [SerializeField] private float knockBackForce;

    [Header("Combo Settings")] 
    public int comboStep = 0;
    public float comboResetTime = 1.0f;
    private float lastAttackTime = 0;
    [SerializeField] private float attackAnimationDuration = 0.3f;

    [Header("Scissor Objects")]
    [SerializeField] private GameObject rightScissor;
    [SerializeField] private GameObject leftScissor;
    [SerializeField] private GameObject combinedScissor;
    [SerializeField] private Transform modelTransform;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip singleScissorClip;
    [SerializeField] private AudioClip doubleScissorClip;

    private Coroutine attackCoroutine;
    private Quaternion originalRotation;

    private void Start()
    {
        if (modelTransform == null) modelTransform = transform;
        originalRotation = modelTransform.localRotation;
        
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Setup scissor components on children if missing
        SetupScissorTrigger(rightScissor);
        SetupScissorTrigger(leftScissor);
        SetupScissorTrigger(combinedScissor);

        // Ensure scissors are off at start
        if (rightScissor) rightScissor.SetActive(false);
        if (leftScissor) leftScissor.SetActive(false);
        if (combinedScissor) combinedScissor.SetActive(false);

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
    private float specialTimer = 0;

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
        Color comboColor = Color.white;
        AudioClip clipToPlay = singleScissorClip;

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        GameObject activeScissor = null;
        Vector3 axis = Vector3.up;
        float angle = 360f;

        switch (comboStep)
        {
            case 1:
                comboColor = Color.white;
                activeScissor = rightScissor;
                axis = Vector3.up;
                angle = -360f;
                clipToPlay = singleScissorClip;
                break;
            case 2:
                currentDamage *= 1.1f;
                comboColor = Color.yellow;
                activeScissor = leftScissor;
                axis = Vector3.up;
                angle = 360f;
                clipToPlay = singleScissorClip;
                break;
            case 3:
                currentDamage *= 1.3f;
                currentCritChance += 0.20f;
                comboColor = Color.red;
                activeScissor = combinedScissor;
                axis = Vector3.right;
                angle = 360f;
                clipToPlay = doubleScissorClip;
                // Note: Combo resets after Hit 3 in ResetCombo() called later or by timeout
                break;
            default:
                ResetCombo();
                comboStep = 1;
                activeScissor = rightScissor;
                axis = Vector3.up;
                angle = -360f;
                clipToPlay = singleScissorClip;
                break;
        }

        if (activeScissor != null)
        {
            Scissors s = activeScissor.GetComponent<Scissors>();
            if (s != null) s.Initialize(currentDamage, currentCritChance);
        }

        if (audioSource != null && clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }

        SetPlayerColor(comboColor);
        attackCoroutine = StartCoroutine(AttackAnimationCoroutine(axis, angle, activeScissor));
        SwitchState(PlayerState.Attacking);
    }

    private IEnumerator AttackAnimationCoroutine(Vector3 axis, float angle, GameObject scissorObj)
    {
        if (scissorObj) scissorObj.SetActive(true);
        
        float elapsed = 0f;
        Quaternion startRot = modelTransform.localRotation;

        while (elapsed < attackAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / attackAnimationDuration;
            
            float currentAngle = Mathf.Lerp(0, angle, percent);
            modelTransform.localRotation = startRot * Quaternion.AngleAxis(currentAngle, axis);
            
            yield return null;
        }

        modelTransform.localRotation = startRot;
        if (scissorObj) scissorObj.SetActive(false);
        attackCoroutine = null;
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
    }

    private void ResetCombo()
    {
        comboStep = 0;
        SetPlayerColor(originalColor);
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
        if (attackCoroutine == null && Time.time - lastAttackTime > 0.1f) 
        {
            if (comboStep >= 3) ResetCombo();
            SwitchState(PlayerState.Idle);
        }
    }

    private void PerformSpecialAttack()
    {
        specialTimer = specialCooldown;
        SetPlayerColor(Color.cyan);
        
        // Visual AOE
        GameObject aoe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aoe.transform.position = transform.position;
        aoe.transform.localScale = Vector3.one * (specialRange * 2);
        Destroy(aoe.GetComponent<Collider>());
        
        Renderer rend = aoe.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        rend.material.color = new Color(0, 1, 1, 0.3f); 
        rend.material.SetFloat("_Surface", 1); 
        rend.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        rend.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        rend.material.SetInt("_ZWrite", 0);
        rend.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Destroy(aoe, 0.4f);

        // Damage + Knockback
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, specialRange, enemyLayer);
        foreach (Collider enemy in hitEnemies)
        {
            Health h = enemy.GetComponent<Health>();
            if (h != null) h.TakeDamage(weaponBaseDamage * 2, transform.position, 10f);
        }

        Debug.Log("SPECIAL ATTACK! AOE Burst.");
        SwitchState(PlayerState.SpecialAttacking);
    }

    private void HandleSpecialAttackState()
    {
        moveDirection = Vector3.zero;
        if (Time.time - lastAttackTime > 0.5f) 
        {
            ResetCombo();
            SwitchState(PlayerState.Idle);
        }
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
        // GUI.Box(new Rect(pos.x, pos.y, size.x, size.y), "");
        // GUI.color = Color.green;
        // GUI.Box(new Rect(pos.x, pos.y, size.x * (playerHealth.currentHealth / playerHealth.maxHealth), size.y), "PLAYER HP: " + (int)playerHealth.currentHealth);
        // GUI.color = Color.white;
        if (specialTimer > 0) GUI.Label(new Rect(pos.x, pos.y + 30, 200, 20), "Special CD: " + specialTimer.ToString("F1") + "s");
        else GUI.Label(new Rect(pos.x, pos.y + 30, 200, 20), "SPECIAL READY (RMB)");
        GUI.Label(new Rect(pos.x, pos.y + 50, 200, 20), "Combo Step: " + comboStep);
        if (GUI.Button(new Rect(pos.x, pos.y + 75, 150, 25), "Reset All Health")) playerHealth.ResetHealth();
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