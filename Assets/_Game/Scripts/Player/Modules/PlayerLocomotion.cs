using UnityEngine;

/// <summary>
/// This class handles the player's locomotion, including movement, gravity, and animation.
/// It provides methods to update the player's position and rotation based on input and state.
/// All references (controller, animator) and tunable stats are delegated to the Player facade / config.
/// </summary>
public class PlayerLocomotion : MonoBehaviour
{

#region Fields

    [Header("References")]
    public Transform modelTransform;

    private Vector3 _moveDirection = Vector3.zero;
    /// <summary>Direction the input asks for, normalized.</summary>
    private Vector3 _inputDirection = Vector3.zero;
    /// <summary>Speed the player is actually moving at right now (m/s).</summary>
    private float _currentSpeed = 0f;
    private float _verticalVelocity = 0f;

    /// <summary>Forward push applied while attacking: where it points.</summary>
    private Vector3 _lungeDirection = Vector3.zero;
    /// <summary>How many meters of the push are still left to travel.</summary>
    private float _lungeRemaining = 0f;
    private Quaternion _originalRotation;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    /// <summary>
    /// How fast a knockback can throw the player upwards (m/s). With the project's gravity
    /// of 25 m/s^2 this caps the lift at about 0.5 m - a hop, not a launch into the sky.
    /// </summary>
    private const float MaxKnockbackLift = 5f;

    private Player _player;

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
        float gravity = _player.gravity;
        Player.PlayerState state = _player.currentState;

        // Stick to ground
        if (Controller.isGrounded && _verticalVelocity < 0)
            _verticalVelocity = -2f;

        // Apply gravity and move the player
        if (state != Player.PlayerState.Attacking && state != Player.PlayerState.SpecialAttacking)
        {
            // Only apply gravity if the player is not dashing
            if (state != Player.PlayerState.Dashing)
                _verticalVelocity -= gravity * Time.deltaTime;
            else
                _verticalVelocity = 0;

            // The dash sets its own velocity, so the ramp must not overwrite it.
            if (state != Player.PlayerState.Dashing)
                UpdateSpeed();

            Vector3 finalMove = _moveDirection;
            finalMove.y = _verticalVelocity;
            Controller.Move(finalMove * Time.deltaTime);
        }
        else
        {
            // Attacking: the player cannot steer, but the attack pushes them forward a little.
            _verticalVelocity -= gravity * Time.deltaTime;
            Controller.Move(new Vector3(0, _verticalVelocity * Time.deltaTime, 0));

            Vector3 lungeStep = ConsumeLungeStep();
            if (lungeStep != Vector3.zero)
            {
                Controller.Move(lungeStep);
            }
        }
    }

    /// <summary>
    /// Pushes the player forward along a direction. The distance is the spec: the push travels
    /// that many meters in total, whatever the attack animation does.
    /// </summary>
    public void StartLunge(Vector3 direction, float distance)
    {
        if (distance <= 0f) return;

        _lungeDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        _lungeRemaining = distance;
    }

    /// <summary>Cancels a push that is still travelling.</summary>
    public void StopLunge()
    {
        _lungeRemaining = 0f;
        _lungeDirection = Vector3.zero;
    }

    /// <summary>
    /// Returns how far the push travels this frame and consumes it. The push is a displacement
    /// rather than a velocity, so the player travels exactly the requested distance.
    /// </summary>
    private Vector3 ConsumeLungeStep()
    {
        if (_lungeRemaining <= 0f) return Vector3.zero;

        float step = _player.lungeSpeed * Time.deltaTime;
        if (step >= _lungeRemaining) step = _lungeRemaining;

        _lungeRemaining -= step;

        if (_lungeRemaining <= 0f) _lungeDirection = Vector3.zero;

        return _lungeDirection * step;
    }

    public void SetMoveDirection(Vector3 direction)
    {
        // The dash and the scripted freezes set the velocity directly, bypassing the ramp.
        _moveDirection = direction;
        _inputDirection = direction == Vector3.zero ? Vector3.zero : direction.normalized;
        _currentSpeed = direction.magnitude;
    }

    /// <summary>
    /// Drops the current velocity back to the ramp, so the player starts from the movement
    /// speed again. Used when the dash ends: the dash velocity must not carry over.
    /// </summary>
    public void ClearMoveOverride()
    {
        _moveDirection = Vector3.zero;
        _inputDirection = Vector3.zero;
        _currentSpeed = 0f;
    }

    public void UpdateRotation(Vector3 direction)
    {
        if (direction != Vector3.zero)
            transform.forward = Vector3.Slerp(transform.forward, new Vector3(direction.x, 0, direction.z), Time.deltaTime * _player.Config.rotationSpeed);
    }

    public void UpdateFromInput(Vector2 input)
    {
        // The input gives a direction and a target speed; the current speed ramps towards it,
        // so the player accelerates into a run and coasts to a stop instead of snapping.
        if (input != Vector2.zero)
        {
            _inputDirection = new Vector3(input.x, 0, input.y).normalized;
            UpdateRotation(_inputDirection);
        }
        else
        {
            _inputDirection = Vector3.zero;
        }

        if (Animator != null)
            Animator.SetBool(IsMovingHash, _inputDirection != Vector3.zero);
    }

    /// <summary>
    /// Ramps the current speed towards the speed the input asks for. Accelerating and
    /// decelerating use separate rates, so the player stops quicker than it starts: that
    /// reads as responsive rather than floaty.
    /// </summary>
    private void UpdateSpeed()
    {
        float targetSpeed = _inputDirection == Vector3.zero ? 0f : _player.speed;
        float rate = targetSpeed > _currentSpeed ? _player.acceleration : _player.deceleration;

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);

        // A tiny leftover would keep the CharacterController creeping after the input stops.
        if (_currentSpeed < 0.01f && targetSpeed == 0f) _currentSpeed = 0f;

        _moveDirection = _inputDirection * _currentSpeed;
    }

    /// <summary>
    /// Applies a knockback: a horizontal push plus a vertical launch.
    /// The vertical part comes from the direction's own Y component, so a caller that wants
    /// the player to stay on the floor just passes a flat direction. It is clamped, because a
    /// big knockback must never launch the player out of the arena.
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force, float duration, float knockBackForce)
    {
        // Normalised first, so the Y component is a ratio (how much of the push goes up)
        // and does not depend on how long the caller's vector happens to be.
        Vector3 pushDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;

        Vector3 horizontalDir = new Vector3(pushDir.x, 0f, pushDir.z).normalized;
        _verticalVelocity = Mathf.Clamp(pushDir.y * force, 0f, MaxKnockbackLift);

        if (horizontalDir != Vector3.zero)
            StartCoroutine(KnockbackCoroutine(horizontalDir, knockBackForce, duration));
    }

    private System.Collections.IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration)
    {
        float t = 0f;
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
        _inputDirection = Vector3.zero;
        _currentSpeed = 0f;
        _verticalVelocity = 0f;
        StopLunge();
    }

    public void HandleDamageReceived(float dmg)
    {
        ResetToIdle();
    }

#endregion
#region Properties

    public Vector3 MoveDirection => _moveDirection;
    public float VerticalVelocity => _verticalVelocity;
    /// <summary>Speed the player is actually moving at right now (m/s).</summary>
    public float CurrentSpeed => _currentSpeed;

    private CharacterController Controller => _player.controller;
    private Animator Animator => _player.animator;

#endregion

}
