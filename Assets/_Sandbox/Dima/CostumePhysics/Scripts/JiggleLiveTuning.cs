using GatorDragonGames.JigglePhysics;
using UnityEngine;

// Sandbox only: makes Play-mode tuning of the jiggle rigs trustworthy. Editing a JiggleRig in the
// Inspector during Play runs JiggleRig.UpdateParameters (v16.0.1), which pushes that one rig's values
// onto every bone of the merged tree and drops "exclude root" - the robe and scarf share one tree here,
// so live edits look broken. This watches the rigs' settings and, when any change, rebuilds the rigs
// (OnRemove/OnInitialize), which assigns per-bone settings correctly. Expect one small pop per edit.
public class JiggleLiveTuning : MonoBehaviour
{
    private JiggleRig[] rigs;
    private string[] snapshots;

    private void Awake()
    {
        rigs = GetComponents<JiggleRig>();
        snapshots = new string[rigs.Length];
        for (int i = 0; i < rigs.Length; i++) snapshots[i] = Snapshot(rigs[i]);
    }

    private void Update()
    {
        bool changed = false;
        for (int i = 0; i < rigs.Length; i++)
        {
            var snapshot = Snapshot(rigs[i]);
            if (snapshot == snapshots[i]) continue;
            snapshots[i] = snapshot;
            changed = true;
        }
        if (!changed) return;
        foreach (var rig in rigs) rig.OnRemove();
        foreach (var rig in rigs) rig.OnInitialize();
    }

    private static string Snapshot(JiggleRig rig) => JsonUtility.ToJson(rig.GetInputParameters());
}
