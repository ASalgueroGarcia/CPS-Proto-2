using UnityEngine;

/// <summary>
/// Floor pit obstacle that blocks normal ground movement while letting a dashing player drop in.
/// Lives in Core so the Player assembly can detect pits without depending on Obstacles.
/// </summary>
public interface IPit
{
    Bounds WorldBounds { get; }
}
