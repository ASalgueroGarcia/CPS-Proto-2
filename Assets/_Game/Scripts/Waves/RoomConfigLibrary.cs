// Library asset holding one RoomConfigData per room tier. Assigned on every room
// prefab's WaveManager; replaces the hardcoded static RoomConfigs dictionary.
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomConfigLibrary", menuName = "Rooms/Config Library")]
public class RoomConfigLibrary : ScriptableObject
{
    [Tooltip("One RoomConfigData per RoomType tier.")]
    public List<RoomConfigData> configs = new List<RoomConfigData>();

    public RoomConfigData Get(RoomType type)
    {
        foreach (var config in configs)
        {
            if (config != null && config.roomType == type) return config;
        }

        Debug.LogError($"[RoomConfigLibrary] No RoomConfigData for tier '{type}' - check Data/Rooms.");
        return null;
    }
}
