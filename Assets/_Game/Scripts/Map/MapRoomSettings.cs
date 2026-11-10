using System;
using UnityEngine;

/// <summary>Numbers that decide which rooms a map node plays. Lives inside <see cref="MapConfig"/>.</summary>
[Serializable]
public class MapRoomSettings
{
    [Header("Fight nodes")]
    [Tooltip("Starting difficulty per map row, first node first. Rows past the end use the last entry.")]
    public RoomType[] rowTiers =
    {
        RoomType.Entrance, RoomType.Entrance, RoomType.Entrance,
        RoomType.Medium, RoomType.Medium,
        RoomType.MediumHard, RoomType.MediumHard, RoomType.MediumHard
    };
    [Min(1)] public int minRooms = 4;
    [Min(1)] public int maxRooms = 5;
    [Tooltip("Rooms played before the difficulty steps up one tier.")]
    [Min(1)] public int roomsPerDifficultyStep = 2;

    [Header("Special rooms inside fight nodes")]
    [Range(0f, 1f)] public float restRoomChance = 0.25f;
    [Range(0f, 1f)] public float shopRoomChance = 0.15f;
    public float restRoomHeal = 40f;
    [Tooltip("Shop visits allowed per map, counting the Shop node.")]
    [Min(0)] public int maxShopsPerScene = 2;

    [Header("Node rewards")]
    [Range(0f, 1f)] public float moreHealthDropChance = 0.45f;
    public float moreButtonsMultiplier = 1.5f;
    [Tooltip("Chance a Treasure node heals instead of paying buttons.")]
    [Range(0f, 1f)] public float treasureHealChance = 0.5f;
    public float treasureHeal = 50f;
    [Tooltip("Buttons a Treasure node pays, per scene of the act.")]
    public int[] treasureButtons = { 10, 12, 14 };
}