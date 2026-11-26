using UnityEngine;

/// <summary>
/// Tuning for enemy knockback and the launch that follows when a knocked-back
/// enemy runs into a wall or obstacle. One asset can be shared by every enemy.
/// </summary>
[CreateAssetMenu(fileName = "EnemyLaunchSettings", menuName = "Enemies/Launch Settings")]
public class EnemyLaunchSettings : ScriptableObject
{
    [Header("Knockback push")]
    [Tooltip("Seconds the push lasts.")]
    [Min(0.01f)] public float pushDuration = 0.2f;
    [Tooltip("Starting push speed (m/s) per point of knockback force. It eases to zero over the push.")]
    [Min(0f)] public float pushSpeedPerForce = 6f;

    [Header("Launch conditions")]
    [Tooltip("Hits weaker than this push the enemy but never launch it.")]
    [Min(0f)] public float minLaunchForce = 5f;
    [Tooltip("Layers that count as walls and obstacles. Trigger colliders are ignored, except explosives, which go off when an enemy is knocked into them.")]
    public LayerMask wallLayers = 1;
    [Tooltip("Radius of the probe that looks for a wall ahead of the enemy.")]
    [Min(0.01f)] public float probeRadius = 0.4f;
    [Tooltip("How far ahead of the enemy a wall still counts as hit.")]
    [Min(0f)] public float probeDistance = 0.6f;
    [Tooltip("Height above the enemy's feet the probe is cast from.")]
    [Min(0f)] public float probeHeight = 0.7f;

    [Header("Launch")]
    [Tooltip("Metres flown per point of knockback force.")]
    [Min(0f)] public float launchDistancePerForce = 0.4f;
    [Tooltip("Longest launch, whatever the force.")]
    [Min(0f)] public float maxLaunchDistance = 5f;
    [Tooltip("Height of the arc at its middle.")]
    [Min(0f)] public float launchHeight = 1.5f;
    [Tooltip("Seconds in the air.")]
    [Min(0.01f)] public float launchDuration = 0.5f;

    public float LaunchDistance(float force) => Mathf.Min(force * launchDistancePerForce, maxLaunchDistance);
}
