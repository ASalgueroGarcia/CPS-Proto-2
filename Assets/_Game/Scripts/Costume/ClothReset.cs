using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps Unity Cloth from breaking on the two kinds of jump it can't simulate:
/// - Enable: Cloth restarts from the bind pose (Sarah's scarf is bound flat, sticking straight back) and the
///   skinning is a frame stale, so the scarf falls through the body. Restarting it a frame later starts it from
///   the draped, animated pose.
/// - Teleport (the door at the start of a room): an instant move reads as huge speed and whips the scarf.
///   ClearTransformMotion drops that move from the simulation.
/// </summary>
[RequireComponent(typeof(Cloth))]
public class ClothReset : MonoBehaviour, ITeleportListener
{
    private Cloth cloth;
    private Coroutine restart;

    private void Awake()
    {
        cloth = GetComponent<Cloth>();
    }

    private void OnEnable()
    {
        restart = StartCoroutine(RestartNextFrame());
    }

    private void OnDisable()
    {
        if (restart == null) return;
        StopCoroutine(restart);
        restart = null;
        cloth.enabled = true; // disabled mid-restart: don't leave the Cloth off for good
    }

    public void OnTeleported()
    {
        if (cloth != null) cloth.ClearTransformMotion(); // null until Awake: an inactive scarf isn't simulating anyway
    }

    private IEnumerator RestartNextFrame()
    {
        cloth.enabled = false;
        yield return null;
        cloth.enabled = true;
        cloth.ClearTransformMotion();
        restart = null;
    }
}
