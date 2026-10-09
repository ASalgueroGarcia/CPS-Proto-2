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

#region Fields

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

    [Header("Configuration")]
    [SerializeField] private PlayerConfig config;
    [SerializeField] private PlayerData data;

    [Header("Damage Bus")]
#pragma warning disable CS0414 // Field assigned but never used (dummy field kept for [Header] decoration)
    [SerializeField] private bool _damageBusHeader = false;
#pragma warning restore CS0414
    public event Action<float> OnDamageReceived;
    public event Action OnHitInterrupted;

    private Dictionary<PlayerState, PlayerStateBase> _states;
    private PlayerStateBase _currentStateInstance;

    public static bool IsPaused = false;

#endregion
#region Unity Lifecycle

    private void Awake()
    {
        // Work on runtime copies so shop upgrades don't mutate the shared assets.
        if (config != null) config = Instantiate(config);
        if (data != null) data = Instantiate(data);

        Locomotion = gameObject.GetOrAddComponent<PlayerLocomotion>();
        Dash = gameObject.GetOrAddComponent<PlayerDashController>();
        Weapon = gameObject.GetOrAddComponent<PlayerWeaponController>();

        Locomotion.Initialize(this);
        Dash.Initialize(this);
        Weapon.Initialize(this);

        // Push stats from PlayerData ScriptableObject into Health.
        if (playerHealth == null) playerHealth = GetComponent<Health>();
        if (playerHealth != null && data != null)
        {
            playerHealth.maxHealth = data.maxHealth;
            playerHealth.currentHealth = data.maxHealth;
        }

        // Player handles its own knockback via PlayerLocomotion (CharacterController),
        // so disable Health's internal knockback. The listener is (un)subscribed in
        // OnEnable/OnDisable to keep the subscription lifecycle symmetric.
        if (playerHealth != null)
        {
            playerHealth.useInternalKnockback = false;
        }

        InitializeStates();
    }

    private void Update()
    {
        if (Weapon != null) Weapon.TickWeapon();

        if (currentState == PlayerState.Attacking || currentState == PlayerState.SpecialAttacking) {
            if (Weapon != null && Time.time - Weapon.GetPlayerLastAttackTime() > attackFailsafeSeconds)
            {
                Debug.LogWarning($"[FAILSAFE] Stuck in attack for >{attackFailsafeSeconds:F1}s. Forcing reset.");
                Weapon.ResetCombo();
                SwitchState(PlayerState.Idle);
            }
        }

        _currentStateInstance?.Tick();

        Locomotion.Tick();
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
        {
            playerHealth.OnDamageTaken.AddListener(HandleHealthDamage);
            playerHealth.OnKnockbackReceived.AddListener(HandleHealthKnockback);
        }

        if (Locomotion != null) OnDamageReceived += Locomotion.HandleDamageReceived;
        if (Weapon != null) OnDamageReceived += Weapon.OnDamageReceived;
        OnDamageReceived += HandleHitInterrupted;
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null) moveAction.action.Disable();
        if (dashAction != null && dashAction.action != null) dashAction.action.Disable();
        if (attackAction != null && attackAction.action != null) attackAction.action.Disable();
        if (specialAttackAction != null && specialAttackAction.action != null) specialAttackAction.action.Disable();

        // Remove events
        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken.RemoveListener(HandleHealthDamage);
            playerHealth.OnKnockbackReceived.RemoveListener(HandleHealthKnockback);
        }

        if (Locomotion != null) OnDamageReceived -= Locomotion.HandleDamageReceived;
        if (Weapon != null) OnDamageReceived -= Weapon.OnDamageReceived;
        OnDamageReceived -= HandleHitInterrupted;
    }

    private void OnDestroy()
    {
        // Destroy runtime copies of the ScriptableObject assets to avoid leaking them
        // across Play mode sessions in the Editor.
        if (config != null) Destroy(config);
        if (data != null) Destroy(data);
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

    // Named handler so we can unsubscribe symmetrically in OnDisable.
    private void HandleHitInterrupted(float dmg) => OnHitInterrupted?.Invoke();

    public void OnPlayerHit()
    {
        OnDamageReceived?.Invoke(0f);
    }

    // Routes Health knockback events through PlayerLocomotion (CharacterController-based).
    private void HandleHealthKnockback(Vector3 source, float force)
    {
        Locomotion?.ApplyKnockback(source, force * 0.1f, 0.2f, force);
    }

    public bool CheckDashInput()
    {
        if (dashAction == null || dashAction.action == null) return false;
        if (!dashAction.action.WasPressedThisFrame()) return false;

        Vector2 input = moveAction != null && moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Dash.StartDash(new Vector3(input.x, 0, input.y).normalized);
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

        if (evt == "ReturnToIdle"
            && (currentState == PlayerState.Attacking || currentState == PlayerState.SpecialAttacking)
            && Time.time - Weapon.GetPlayerLastAttackTime() > 0.15f)
        {
            Weapon.ResetCombo();
            SwitchState(PlayerState.Idle);
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
#region Properties

    public PlayerConfig Config => config;

    public PlayerData Data => data;

    public float speed
    {
        get => config != null ? config.speed : 0f;
        set { if (config != null) config.speed = value; }
    }

    public float dashSpeed
    {
        get => config != null ? config.dashSpeed : 0f;
        set { if (config != null) config.dashSpeed = value; }
    }

    public float gravity => config != null ? config.gravity : 0f;

    public float runAnimationSpeed => config != null ? config.runAnimationSpeed : 1.5f;

    public float dashDuration => config != null ? config.dashDuration : 0.2f;

    public float attackFailsafeSeconds => config != null ? config.attackFailsafeSeconds : 3.0f;

    public PlayerLocomotion Locomotion { get; private set; }
    public PlayerDashController Dash { get; private set; }
    public PlayerWeaponController Weapon { get; private set; }

    public PlayerModules Modules => new PlayerModules(Locomotion, Dash, Weapon);

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

    public float SpecialTimer => Weapon?.CurrentWeapon?.SpecialTimer ?? 0f;

    public int ComboStep => Weapon?.CurrentWeapon?.CurrentComboStep ?? 0;

#endregion
}
