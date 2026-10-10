using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This class handles the player's dash ability, including movement, invulnerability, and visual/audio effects during the dash.
/// References to Health and Animator are delegated to the Player facade.
/// Dash tunables (speed, duration) are read from the shared PlayerConfig ScriptableObject.
/// </summary>
public class PlayerDashController : MonoBehaviour
{

#region Fields

    [Header("References")]
    [SerializeField] private TrailRenderer dashTrail;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> dashClips;

    private Vector3 _dashDirection = Vector3.zero;
    private float _dashTimer = 0f;

    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");

    private Player _player;

#endregion
#region Methods

    public void Initialize(Player player)
    {
        _player = player;
        if (dashTrail == null) dashTrail = GetComponent<TrailRenderer>();
    }

    public void StartDash(Vector3 moveDirection)
    {
        _dashDirection = moveDirection != Vector3.zero ? moveDirection : _player.transform.forward;
        _dashTimer = _player.dashDuration;
        _dashDirection.y = 0;

        if (SoundManager.Instance != null && dashClips != null && dashClips.Count > 0)
            SoundManager.Instance.PlayRandomSound(dashClips);

        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = true;
        }

        if (Animator != null) Animator.SetBool(IsDashingHash, true);

        if (PlayerHealth != null)
            PlayerHealth.isInvulnerable = true;

        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex != -1) Physics.IgnoreLayerCollision(_player.gameObject.layer, enemyLayerIndex, true);
    }

    public void Tick()
    {
        _player.Locomotion.SetMoveDirection(_dashDirection * _player.dashSpeed);
        _dashTimer -= Time.deltaTime;
    }

    public bool IsDashFinished()
    {
        return _dashTimer <= 0f;
    }

    public void EndDash()
    {
        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = false;
        }

        if (Animator != null) Animator.SetBool(IsDashingHash, false);

        if (PlayerHealth != null)
            PlayerHealth.isInvulnerable = false;

        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex != -1) Physics.IgnoreLayerCollision(_player.gameObject.layer, enemyLayerIndex, false);
    }

#endregion
#region Properties

    public Vector3 CurrentDirection => _dashDirection;
    public float DashTimer => _dashTimer;

    private Health PlayerHealth => _player.playerHealth;
    private Animator Animator => _player.animator;

#endregion

}
