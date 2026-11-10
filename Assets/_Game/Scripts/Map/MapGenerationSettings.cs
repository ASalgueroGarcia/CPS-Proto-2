using System;
using UnityEngine;

/// <summary>Numbers the map generator rolls with. Lives inside <see cref="MapConfig"/>.</summary>
[Serializable]
public class MapGenerationSettings
{
    [Tooltip("Paths drawn from the first node to the final fight.")]
    [Min(1)] public int pathCount = 4;

    [Header("Shop (rows count from 0 at the first node)")]
    [Tooltip("First row the single Shop node may sit on.")]
    public int shopFirstRow = 2;
    [Tooltip("Last row the single Shop node may sit on.")]
    public int shopLastRow = 5;

    [Header("Node type weights")]
    [Min(0)] public int combatWeight = 40;
    [Min(0)] public int moreHealthWeight = 20;
    [Min(0)] public int moreButtonsWeight = 20;
    [Min(0)] public int treasureWeight = 20;
}