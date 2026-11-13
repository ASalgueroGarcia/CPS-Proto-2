using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sits on a room layout that has pits. The layout brings its own floor with the holes cut out,
/// so the room scene's own floor surfaces are hidden while the layout is loaded. Only their look
/// is hidden: their colliders stay, and the pits themselves block walking.
/// </summary>
public class PitFloor : MonoBehaviour
{
    [Tooltip("Names of the floor objects in the room scene: the walkable plane and the stage surface under it.")]
    [SerializeField] private string[] sceneFloorNames = { "Ground", "Arena Flat" };

    private readonly List<Renderer> hidden = new List<Renderer>();

    private void OnEnable()
    {
        foreach (string floorName in sceneFloorNames)
        {
            GameObject floor = GameObject.Find(floorName);
            if (floor == null || !floor.TryGetComponent(out Renderer surface) || !surface.enabled) continue;

            surface.enabled = false;
            hidden.Add(surface);
        }
    }

    private void OnDisable()
    {
        foreach (Renderer surface in hidden)
        {
            if (surface != null) surface.enabled = true;
        }

        hidden.Clear();
    }
}
