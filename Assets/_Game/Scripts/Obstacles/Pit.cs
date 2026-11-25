using UnityEngine;

/// <summary>
/// Marks a floor pit obstacle. Its solid collider and NavMeshObstacle block normal walking and
/// enemy pathfinding while projectiles pass over on Ignore Raycast; dashing into it makes the player fall.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Pit : MonoBehaviour, IPit
{
    private Collider pitCollider;

    public Collider PitCollider
    {
        get
        {
            if (pitCollider == null)
                pitCollider = GetComponent<Collider>();

            return pitCollider;
        }
    }

    public Bounds WorldBounds => PitCollider != null
        ? PitCollider.bounds
        : new Bounds(transform.position, Vector3.Scale(Vector3.one, transform.lossyScale));

    private void Awake()
    {
        pitCollider = GetComponent<Collider>();
    }
}
