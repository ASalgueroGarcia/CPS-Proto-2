using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Core.Utils;

/// <summary>
/// Structure to hold references to the player's modules (locomotion, dash, weapon).
/// (Just a convenient way to pass around the modules as a single object. :) )
/// </summary>
public readonly struct PlayerModules
{
    public readonly PlayerLocomotion Locomotion;
    public readonly PlayerDashController Dash;
    public readonly PlayerWeaponController Weapon;

    public PlayerModules(PlayerLocomotion locomotion, PlayerDashController dash, PlayerWeaponController weapon)
    {
        Locomotion = locomotion;
        Dash = dash;
        Weapon = weapon;
    }
}

/// <summary>
/// Main player class that manages the player's state, input, and interactions with modules like locomotion, dash, and weapon.
/// It also handles player health, damage reception, and state transitions.
/// </summary>
/// Facade Power b*tch
public class Player : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Moving,
        Dashing,
        Attacking,
        SpecialAttacking
    }

#region Fields and Properties
    
    [Header("State Tracker")]
    public PlayerState currentState = PlayerState.Idle;

    [Header("Components")]
    public CharacterController controller;
    public Renderer bodyRenderer;
    public Health playerHealth;
    public AudioSource audioSource;
    public Animator animator;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference dashAction;
    public InputActionReference attackAction;
    public InputActionReference specialAttackAction;

    [Header("Identification")]
    public LayerMask enemyLayer;

    [Header("Movement Stats (read by facade for UI)")]
    public float speed = 14f;
    public float dashSpeed = 30f;
    public float gravity = 25f;
    [SerializeField] private float runAnimationSpeed = 1.5f;

    [Header("Dash")]
    public float dashDuration = 0.2f;
    public float attackFailsafeSeconds = 3.0f;

    [Header("Damage Bus")]
    [SerializeField] private bool _damageBusHeader = false;
    public event Action<float> OnDamageReceived;
    public event Action OnHitInterrupted;

    public PlayerLocomotion Locomotion { get; private set; }
    public PlayerDashController Dash { get; private set; }
    public PlayerWeaponController Weapon { get; private set; }

    public PlayerModules Modules => new PlayerModules(Locomotion, Dash, Weapon);

    private Dictionary<PlayerState, PlayerStateBase> _states;
    private PlayerStateBase _currentStateInstance;
    private PlayerState _previousState;

    public static bool IsPaused = false;

    public float GetPlayerDamage() => Weapon != null ? Weapon.GetPlayerDamage() : 0f;

    public float WeaponBaseDamage
    {
        get => Weapon?.CurrentWeapon?.BaseDamage ?? 0f;
        set { if (Weapon?.CurrentWeapon != null) Weapon.CurrentWeapon.BaseDamage = value; }
    }

    public float BaseCritChance
    {
        get => Weapon?.CurrentWeapon?.BaseCritChance ?? 0f;
        set { if (Weapon?.CurrentWeapon != null) Weapon.CurrentWeapon.BaseCritChance = value; }
    }

    // Time remaining for the special attack cooldown, if applicable.
    public float SpecialTimer => Weapon?.CurrentWeapon?.SpecialTimer ?? 0f;
    // Current combo step of the weapon, if applicable.
    public int ComboStep => Weapon?.CurrentWeapon?.CurrentComboStep ?? 0;

#endregion
#region Unity Lifecycle

    private void Awake()
    {
        Locomotion = gameObject.GetOrAddComponent<PlayerLocomotion>();
        Dash = gameObject.GetOrAddComponent<PlayerDashController>();
        Weapon = gameObject.GetOrAddComponent<PlayerWeaponController>();

        Locomotion.Initialize(this);
        Dash.Initialize(this);
        Weapon.Initialize(this);

        InitializeStates();
    }

    private void Update()
    {
        if (Weapon != null) Weapon.TickWeapon();

        if (currentState == PlayerState.Attacking || currentState == PlayerState.SpecialAttacking) {
            if (Weapon != null) 
                Weapon.TickFallback(attackFailsafeSeconds);
        }

        if (Weapon != null && Weapon.ShouldResetCombo()) {
            Weapon.CurrentWeapon?.ResetCombo();
        }

        _currentStateInstance?.Tick();

        Locomotion.currentState = currentState;
        Locomotion.Tick();

        // Handle invulnerability during dashing
        if (currentState == PlayerState.Dashing && _previousState != PlayerState.Dashing) 
        {
            if (playerHealth != null)
                playerHealth.isInvulnerable = true;
            
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            
            if (enemyLayerIndex != -1) 
                Physics.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, true);
        }
        // Handle exiting invulnerability when leaving the dashing state
        else if (_previousState == PlayerState.Dashing && currentState != PlayerState.Dashing)
        {
            if (playerHealth != null) 
                playerHealth.isInvulnerable = false;
            
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            
            if (enemyLayerIndex != -1) 
                Physics.IgnoreLayerCollision(gameObject.layer, enemyLayerIndex, false);
        }

        _previousState = currentState; // Update the previous state for the next frame
    }

    private void OnEnable()
    {
        if (playerHealth == null) playerHealth = GetComponent<Health>();

        if (moveAction != null && moveAction.action != null) moveAction.action.Enable();
        if (dashAction != null && dashAction.action != null) dashAction.action.Enable();
        if (attackAction != null && attackAction.action != null) attackAction.action.Enable();
        if (specialAttackAction != null && specialAttackAction.action != null) specialAttackAction.action.Enable();

        // Events
        if (playerHealth != null)
            playerHealth.OnDamageTaken.AddListener(HandleHealthDamage);

        if (Locomotion != null) OnDamageReceived += Locomotion.HandleDamageReceived;
        if (Weapon != null) OnDamageReceived += Weapon.OnDamageReceived;
        OnDamageReceived += _ => OnHitInterrupted?.Invoke();
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null) moveAction.action.Disable();
        if (dashAction != null && dashAction.action != null) dashAction.action.Disable();
        if (attackAction != null && attackAction.action != null) attackAction.action.Disable();
        if (specialAttackAction != null && specialAttackAction.action != null) specialAttackAction.action.Disable();
        
        // Remove events
        if (playerHealth != null)
            playerHealth.OnDamageTaken.RemoveListener(HandleHealthDamage);

        if (Locomotion != null) OnDamageReceived -= Locomotion.HandleDamageReceived;
        if (Weapon != null) OnDamageReceived -= Weapon.OnDamageReceived;
    }

#endregion
#region State Management

    private void InitializeStates()
    {
        _states = new Dictionary<PlayerState, PlayerStateBase>
        {
            { PlayerState.Idle, new PlayerIdleState() },
            { PlayerState.Moving, new PlayerMovingState() },
            { PlayerState.Dashing, new PlayerDashingState() },
            { PlayerState.Attacking, new PlayerAttackingState() },
            { PlayerState.SpecialAttacking, new PlayerSpecialAttackingState() }
        };

        foreach (var statePair in _states)
        {
            statePair.Value.SetPlayer(this);
        }

        _currentStateInstance = _states[currentState];
        _currentStateInstance.Enter();
    }

    public void SwitchState(PlayerState newState)
    {
        if (currentState == newState) return;

        _previousState = currentState;
        _currentStateInstance?.Exit();
        currentState = newState;
        _currentStateInstance = _states[newState];
        ApplyAnimatorSpeed(newState);
        _currentStateInstance.Enter();
    }

#endregion
#region Handlers and Input Checks

    private void HandleHealthDamage(float dmg)
    {
        OnDamageReceived?.Invoke(dmg);
    }

    public void OnPlayerHit()
    {
        OnDamageReceived?.Invoke(0f);
    }

    public bool CheckDashInput()
    {
        if (dashAction == null || dashAction.action == null) return false;
        if (!dashAction.action.WasPressedThisFrame()) return false;

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 dir = new Vector3(input.x, 0, input.y);
        Dash.StartDash(dir);
        SwitchState(PlayerState.Dashing);
        return true;
    }

    public bool CheckAttackInput()
    {
        if (attackAction == null || attackAction.action == null) return false;
        if (!attackAction.action.WasPressedThisFrame()) return false;

        Weapon.OnAttackInput();
        return true;
    }

    public bool CheckHeavyAttackInput()
    {
        if (specialAttackAction == null || specialAttackAction.action == null) return false;
        if (!specialAttackAction.action.WasPressedThisFrame()) return false;

        if (Weapon?.CurrentWeapon != null && !Weapon.CurrentWeapon.CanUseSpecial) return false;

        Weapon.OnHeavyAttackInput();
        return true;
    }

#endregion
#region Animation and Visual Effects

    private void ApplyAnimatorSpeed(PlayerState state)
    {
        if (animator == null) return;

        float attackSpeed = Weapon?.CurrentWeapon?.AttackAnimationSpeed ?? 1.5f;
        float specialSpeed = Weapon?.CurrentWeapon?.SpecialAnimationSpeed ?? 1.2f;

        switch (state)
        {
            case PlayerState.Moving:
                animator.speed = runAnimationSpeed;
                break;
            case PlayerState.Attacking:
                animator.speed = attackSpeed;
                break;
            case PlayerState.SpecialAttacking:
                animator.speed = specialSpeed;
                break;
            default:
                animator.speed = 1.0f;
                break;
        }
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        Locomotion.ApplyKnockback(direction, force, duration, force);
    }

    public void OnAnimationEvent(string evt)
    {
        Weapon?.OnAnimationEvent(evt);

        if (evt == "ReturnToIdle")
        {
            if (Time.time - Weapon.GetPlayerLastAttackTime() > 0.15f)
            {
                SwitchState(PlayerState.Idle);
            }
        }
    }

#endregion
#region Debugging

    [ContextMenu("Debug: Play Attack 3")]
    public void DebugPlayAttack3()
    {
        Weapon?.OnAnimationEvent("DebugPlayAttack3");
        SwitchState(PlayerState.Attacking);
    }

#endregion
}
