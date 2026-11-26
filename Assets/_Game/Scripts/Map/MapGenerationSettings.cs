using System;
using UnityEngine;

[Serializable]
public class MapGenerationSettings
{
    [Tooltip("Paths drawn from the first node to the final fight.")]
    [Min(1)] public int pathCount = 4;

    [Header("Shop (rows count from 0 at the first node)")]
    [Tooltip("First row the single Shop node may sit on.")]
    [Min(1)] public int shopFirstRow = 2;
    [Tooltip("Last row the single Shop node may sit on.")]
    [Min(1)] public int shopLastRow = 5;

    [Header("Elite (rows count from 0 at the first node)")]
    [Tooltip("First row an Elite node may sit on (GDD row 4 = row 3).")]
    [Min(1)] public int eliteFirstRow = 3;
    [Tooltip("Most Elite nodes one map may hold.")]
    [Min(0)] public int maxElitesPerMap = 2;

    [Header("Node type weights")]
    [Min(0)] public int combatWeight = 40;
    [Min(0)] public int healthWeight = 20;
    [Min(0)] public int treasureWeight = 15;
    [Min(0)] public int mysteryWeight = 15;
    [Min(0)] public int eliteWeight = 10;
}
