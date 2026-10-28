using System.Collections;
using UnityEngine;

// Sandbox only: Sarah's scarf mesh is bound flat and sticking straight back; the animation pose drapes it. When the
// character is re-enabled, Unity Cloth restarts from that bind pose (the skinning is a frame stale) and the scarf falls
// through the body colliders. Restarting the Cloth one frame later starts it from the draped, skinned pose instead.
[RequireComponent(typeof(Cloth))]
public class ClothEnableReset : MonoBehaviour
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

    private IEnumerator RestartNextFrame()
    {
        cloth.enabled = false;
        yield return null;
        cloth.enabled = true;
        cloth.ClearTransformMotion();
        restart = null;
    }
}
