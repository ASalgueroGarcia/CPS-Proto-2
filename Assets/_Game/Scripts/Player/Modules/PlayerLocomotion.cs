using UnityEngine;

/// <summary>
/// This class handles the player's locomotion, including movement, gravity, and animation.
/// It provides methods to update the player's position and rotation based on input and state.
/// All references (controller, animator) are delegated to the Player facade.
/// </summary>
public class PlayerLocomotion : MonoBehaviour
{

#region Fields and Properties

    [Header("References")]
    public Transform modelTransform;

    [Header("Movement Stats")]
    public float speed = 14f;
    public float gravity = 25f;
    [SerializeField] private float runAnimationSpeed = 1.5f;

    [Header("State (set by Player)")]
    public Player.PlayerState currentState;

    private Vector3 _moveDirection = Vector3.zero;
    private float _verticalVelocity = 0f;
    private Quaternion _originalRotation;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Player _player;

    private CharacterController Controller => _player.controller;
    private Animator Animator => _player.animator;

    public Vector3 MoveDirection => _moveDirection;
    public float VerticalVelocity => _verticalVelocity;

#endregion
#region Methods

    public void Initialize(Player player)
    {
        _player = player;
        if (modelTransform == null) modelTransform = player.transform;
        _originalRotation = modelTransform.localRotation;
    }

    public void Tick()
    {
        // Stick to ground
        if (Controller.isGrounded && _verticalVelocity < 0)
            _verticalVelocity = -2f;

        // Apply gravity and move the player
        if (currentState != Player.PlayerState.Attacking && currentState != Player.PlayerState.SpecialAttacking)
        {
            // Only apply gravity if the player is not dashing
            if (currentState != Player.PlayerState.Dashing)
                _verticalVelocity -= gravity * Time.deltaTime;
            else
                _verticalVelocity = 0;

            Vector3 finalMove = _moveDirection;
            finalMove.y = _verticalVelocity;
            Controller.Move(finalMove * Time.deltaTime);
        }
        else
        {
            _verticalVelocity -= gravity * Time.deltaTime;
            Controller.Move(new Vector3(0, _verticalVelocity * Time.deltaTime, 0));
        }
    }

    public void SetMoveDirection(Vector3 direction)
    {
        _moveDirection = direction;
    }

    public void UpdateRotation(Vector3 direction)
    {
        if (direction != Vector3.zero)
            transform.forward = new Vector3(direction.x, 0, direction.z);
    }

    public void UpdateFromInput(Vector2 input)
    {
        if (input != Vector2.zero)
        {
            _moveDirection = new Vector3(input.x, 0, input.y).normalized * speed;
            UpdateRotation(_moveDirection);
        }
        else
        {
            _moveDirection = Vector3.zero;
        }

        if (Animator != null)
            Animator.SetBool(IsMovingHash, _moveDirection != Vector3.zero);
    }

    public void ApplyKnockback(Vector3 direction, float force, float duration, float knockBackForce)
    {
        _verticalVelocity = force * 1.5f;
        Vector3 horizontalDir = new Vector3(direction.x, 0, direction.z).normalized;
        StartCoroutine(KnockbackCoroutine(horizontalDir, force, duration, knockBackForce));
    }

    private System.Collections.IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration, float knockBackForce)
    {
        float t = 0f;
        force = knockBackForce;
        while (t < duration)
        {
            Controller.Move(direction * force * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
    }

    public void ResetToIdle()
    {
        _moveDirection = Vector3.zero;
        _verticalVelocity = 0f;
    }

    public void HandleDamageReceived(float dmg)
    {
        ResetToIdle();
    }

#endregion

}
