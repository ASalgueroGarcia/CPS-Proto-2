using UnityEngine;

[CreateAssetMenu(fileName = "FlowerSettings", menuName = "Enemies/Flower Settings")]
public class FlowerSettings : ScriptableObject
{
    [Header("General")]
    [Min(1f)] public float maxHealth = 10f;
    [Range(1, 3)] public int defaultTier = 2;
    [Min(0.5f)] public float detectionRange = 6f;
    [Min(0.5f)] public float growDuration = 6f;
    [Min(1f)] public float grownScaleMultiplier = 1.25f;

    [Header("Bite (All Tiers)")]
    [Min(0.1f)] public float biteRange = 1.5f;
    [Min(0f)] public float biteDamage = 15f;
    [Min(0.05f)] public float biteWindup = 0.5f;
    [Min(0.1f)] public float biteCooldown = 1.5f;

    [Header("Normal Projectile (All Tiers)")]
    [Min(0.5f)] public float projectileRange = 6f;
    [Min(0f)] public float projectileDamage = 10f;
    [Min(0.5f)] public float projectileSpeed = 5f;
    [Min(0.1f)] public float projectileCooldown = 2f;
    [Min(0.05f)] public float projectileSize = 0.35f;

    [Header("Pollinating Attack (Tier 2+)")]
    [Range(1, 3)] public int pollinatingMinTier = 2;
    [Min(0f)] public float pollinatingDamage = 10f;
    [Min(0.2f)] public float pollinatingArcHeight = 2.5f;
    [Min(0.1f)] public float pollinatingFlightTime = 0.9f;
    [Min(1)] public int maxSpawnedFlowersAlive = 2;

    [Header("Burrow (Tier 3+)")]
    [Range(1, 3)] public int burrowMinTier = 3;
    [Min(0.1f)] public float burrowWindup = 1f;
    [Min(0.1f)] public float burrowUndergroundTime = 2f;
    [Min(0.1f)] public float burrowCooldown = 3f;
    [Min(0.5f)] public float burrowMaxHopDistance = 6f;
    [Min(0.1f)] public float resurfaceMinOffset = 1f;
    [Min(0.2f)] public float resurfaceMaxOffset = 2f;
    [Min(1)] public int resurfaceAttempts = 5;
    [Min(0.1f)] public float sinkDepth = 1.5f;
    [Min(0.1f)] public float crackWidth = 0.5f;
}
