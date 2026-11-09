using GatorDragonGames.JigglePhysics;
using UnityEngine;

// Sandbox only: tells JigglePhysics about the mover's instant move home, so the robe and scarf are carried
// along instead of reading the jump as speed (JiggleRig.Teleport, added in our fork).
[RequireComponent(typeof(CostumeTestMover))]
public class JiggleTeleportReset : MonoBehaviour
{
    private CostumeTestMover mover;
    private JiggleRig[] rigs;

    private void Awake()
    {
        mover = GetComponent<CostumeTestMover>();
        rigs = GetComponents<JiggleRig>();
    }

    private void OnEnable()
    {
        mover.Teleported += OnTeleported;
    }

    private void OnDisable()
    {
        mover.Teleported -= OnTeleported;
    }

    private void OnTeleported()
    {
        foreach (var rig in rigs) rig.Teleport();
    }
}
