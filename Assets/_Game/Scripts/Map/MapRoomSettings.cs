using System;
using UnityEngine;

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
    [Tooltip("Shop rooms allowed per map inside fight nodes (excludes the single Shop node).")]
    [Min(0)] public int maxShopsPerScene = 2;

    [Header("Node rewards")]
    [Tooltip("HP restored by a Health node's calm rest room.")]
    [Min(0f)] public float healthNodeHeal = 60f;
    [Tooltip("Chance a Treasure node heals instead of paying buttons.")]
    [Range(0f, 1f)] public float treasureHealChance = 0.5f;
    public float treasureHeal = 50f;
    [Tooltip("Buttons a Treasure node pays, per scene of the act.")]
    public int[] treasureButtons = { 10, 12, 14 };

    [Header("Mystery nodes")]
    [Min(0)] public int mysteryFightWeight = 30;
    [Min(0)] public int mysteryTreasureWeight = 25;
    [Min(0)] public int mysteryDealWeight = 25;
    [Min(0)] public int mysteryEliteWeight = 10;
    [Min(0)] public int mysteryNarrativeWeight = 10;
    [Tooltip("Below this HP ratio (0-1) a Mystery node never rolls Elite.")]
    [Range(0f, 1f)] public float mysteryEliteMinHealthRatio = 0.40f;
    [Tooltip("First map row (0-based) where a Mystery node may roll Elite.")]
    [Min(1)] public int mysteryEliteFirstRow = 3;
}
