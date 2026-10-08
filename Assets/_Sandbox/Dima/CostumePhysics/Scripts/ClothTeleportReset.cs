using UnityEngine;

// Sandbox only: Unity Cloth reads an instant move as huge speed and whips. ClearTransformMotion is
// Unity's own fix for teleports: it drops the pending transform change from the simulation.
[RequireComponent(typeof(CostumeTestMover))]
public class ClothTeleportReset : MonoBehaviour
{
    private CostumeTestMover mover;
    private Cloth cloth;

    private void Awake()
    {
        mover = GetComponent<CostumeTestMover>();
        cloth = GetComponentInChildren<Cloth>();
    }

    private void OnEnable()
    {
        mover.Teleported += cloth.ClearTransformMotion;
    }

    private void OnDisable()
    {
        mover.Teleported -= cloth.ClearTransformMotion;
    }
}
