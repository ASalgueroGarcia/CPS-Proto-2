// TUNING DATA for one room tier (Phase 5 of the audit: the hardcoded RoomConfigs static
// table in WaveManager.cs becomes designer-editable assets in Data/Rooms, mirroring
// how EnemyData solved the same problem for enemies).
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomConfig_Entrance", menuName = "Rooms/Room Config")]
public class RoomConfigData : ScriptableObject
{
    [Tooltip("Which tier this config drives.")]
    public RoomType roomType;

    [Tooltip("Total spawn budget per wave.")]
    public int budget;

    [Tooltip("Waves per room at this tier.")]
    public int maxWaves;

    [Tooltip("Enemy types the wave budget can spend on.")]
    public List<EnemyType> enemyPool = new List<EnemyType>();

    [Tooltip("Coins spawned after the final wave.")]
    public int expectedCurrency;

    [Tooltip("Chance (0-1) of a health drop after the final wave.")]
    [Range(0f, 1f)]
    public float healthDropChance;
}
