using UnityEngine;

/// <summary>
/// ScriptableObject holding the player's character stats (maxHealth, etc.).
/// Mirrors the pattern used by <see cref="EnemyData"/> for enemies: a single
/// source-of-truth asset pushed into the entity's components on Awake.
/// Separate from <see cref="PlayerConfig"/> which holds runtime tunables
/// (speed, dashSpeed, gravity...) that change frequently per frame.
/// </summary>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Player/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Maximum health points.")]
    public float maxHealth = 400f;
}
