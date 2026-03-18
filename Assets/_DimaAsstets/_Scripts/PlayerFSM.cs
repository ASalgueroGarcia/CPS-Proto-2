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
        Attacking
    }

    [Header("State Tracker")]
    public PlayerState currentState = PlayerState.Idle;

    [Header("Components")]
    public CharacterController controller;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference dashAction;
    public InputActionReference attackAction;

    [Header("Stats")]
    public float speed = 8f;
    public float dashSpeed = 30f;
    public float gravity = 25f;
    
    [Header("Dash")]
    public float dashDuration = 0.2f;
    public float dashTimer = 0;

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 dashDirection = Vector3.zero;
    private float verticalVelocity = 0f;

    // --- 2. SETUP INPUTS ---
    
    private void OnEnable()
    {
        moveAction.action.Enable();
        dashAction.action.Enable();
        attackAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        dashAction.action.Disable();
        attackAction.action.Disable();
    }

    void Update()
    {
        // --- 3. THE STATE MACHINE ---
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
        }

        ApplyMovement();
    }

    private void ApplyMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Small downward force to stay grounded
        }

        if (currentState != PlayerState.Dashing)
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = 0; // No gravity during dash for Hades feel
        }

        Vector3 finalMove = moveDirection;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }

    // --- 4. STATE LOGIC ---

    private void HandleIdleState()
    {
        moveDirection = Vector3.zero;
        
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        if (input != Vector2.zero)
        {
            SwitchState(PlayerState.Moving);
            return;
        }
        
        if (dashAction.action.WasPressedThisFrame())
        {
            StartDash(transform.forward);
            return;
        }

        if (attackAction.action.WasPressedThisFrame())
        {
            SwitchState(PlayerState.Attacking);
            return;
        }
    }

    private void HandleMovingState()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        
        if (input == Vector2.zero)
        {
            SwitchState(PlayerState.Idle);
            return;
        }

        moveDirection = new Vector3(input.x, 0, input.y).normalized * speed;
        
        // Rotate towards movement direction
        if (moveDirection != Vector3.zero)
        {
            transform.forward = new Vector3(moveDirection.x, 0, moveDirection.z);
        }

        if (dashAction.action.WasPressedThisFrame())
        {
            StartDash(moveDirection.normalized);
            return;
        }

        if (attackAction.action.WasPressedThisFrame())
        {
            SwitchState(PlayerState.Attacking);
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

        if (dashTimer <= 0)
        {
            SwitchState(PlayerState.Idle);
        }
    }

    private void HandleAttackingState()
    {
        moveDirection = Vector3.zero;
        Debug.Log("Attacking!");
        
        // Simple attack duration for now
        if (attackAction.action.WasReleasedThisFrame() || !attackAction.action.IsPressed())
        {
            SwitchState(PlayerState.Idle);
        }
    }

    // --- 5. HELPER METHODS ---

    private void SwitchState(PlayerState newState)
    {
        currentState = newState;
    }
}