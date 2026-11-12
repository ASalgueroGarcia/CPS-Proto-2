using UnityEngine;

/// <summary>
/// Sits on a room layout that has pits. The layout brings its own floor with the holes cut out,
/// so the arena's plain floor is hidden while the layout is loaded. Only its look is hidden:
/// its collider stays, and the pits themselves block walking.
/// </summary>
public class PitFloor : MonoBehaviour
{
    [Tooltip("Name of the arena's floor object in the room scene.")]
    [SerializeField] private string arenaFloorName = "Ground";

    private Renderer arenaFloor;

    private void OnEnable()
    {
        GameObject floor = GameObject.Find(arenaFloorName);
        if (floor == null || !floor.TryGetComponent(out arenaFloor)) return;

        arenaFloor.enabled = false;
    }

    private void OnDisable()
    {
        if (arenaFloor != null) arenaFloor.enabled = true;
    }
}
