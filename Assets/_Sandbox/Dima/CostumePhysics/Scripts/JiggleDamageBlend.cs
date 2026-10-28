using System;
using System.Linq;
using GatorDragonGames.JigglePhysics;
using UnityEngine;

// Sandbox only: Miguel bakes the robe's secondary motion into the clips, except TakeDamage_01/02. In those states the
// runtime Jiggle fades in; everywhere else it holds the animated (baked) pose. JigglePhysics ignores its own blend input
// (the package always uses 1), so the fade drives the parameters instead: at 0, stiffness 1 with no soften, stretch or
// gravity pins every bone to the animated pose; at 1, the rigs' tuning. The values change continuously, so fading in on
// top of the baked pose can't pop.
public class JiggleDamageBlend : MonoBehaviour
{
    [Serializable]
    private struct Tuning
    {
        public float stiffness, soften, stretch, gravity;
    }

    [SerializeField] private Animator animator;
    [Tooltip("The robe rigs to fade. Empty = every JiggleRig on this object.")]
    [SerializeField] private JiggleRig[] rigs;
    [Tooltip("Animator states (base layer) that have no baked cloth motion.")]
    [SerializeField] private string[] statesWithoutBakedCloth = { "TakeDamage_01", "TakeDamage_02" };
    [Tooltip("Temporary, until Miguel bakes the cloth motion into TakeDamage_01/02: how much runtime jiggle plays in " +
             "those states (0 = baked pose only, 1 = the rigs' full tuning).")]
    [SerializeField, Range(0f, 1f)] private float targetBlend = 0.5f;
    [Tooltip("Seconds to fade in when a state starts and out when it ends.")]
    [SerializeField, Min(0.001f)] private float fadeTime = 0.1f;
    // The rigs' own tuning, saved in the Editor: at runtime the rigs hold the faded values, so a copy of a live
    // character (Instantiate) would otherwise read "pinned" as its tuning.
    [SerializeField, HideInInspector] private Tuning[] tuning;

    private int[] stateHashes;

    public float Blend { get; private set; }
    public float TargetBlend => targetBlend;

    private void OnValidate()
    {
        if (!Application.isPlaying) CaptureTuning();
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        if (stateHashes != null) Apply(0f); // switched off = baked pose only, never stuck mid-fade
    }

    private void Update()
    {
        Tick(InStateWithoutBakedCloth(), Time.deltaTime);
    }

    // True in one of the states, and from the start of a crossfade into one until the start of the crossfade out.
    public bool InStateWithoutBakedCloth()
    {
        Initialize();
        if (animator == null || !animator.isActiveAndEnabled) return false;
        if (animator.IsInTransition(0)) return IsUncovered(animator.GetNextAnimatorStateInfo(0).shortNameHash);
        return IsUncovered(animator.GetCurrentAnimatorStateInfo(0).shortNameHash);
    }

    // Moves the blend toward the target at a rate that covers 0 -> targetBlend in fadeTime.
    public void Tick(bool inState, float deltaTime)
    {
        Initialize();
        float target = inState ? targetBlend : 0f;
        float rate = Mathf.Max(targetBlend, 0.01f) / fadeTime;
        float next = Mathf.MoveTowards(Blend, target, rate * deltaTime);
        if (next != Blend) Apply(next);
    }

    // Runs from Awake, or on first use where Awake doesn't run (EditMode tests).
    private void Initialize()
    {
        if (stateHashes != null) return;
        if (animator == null) animator = GetComponent<Animator>();
        if (rigs == null || rigs.Length == 0) rigs = GetComponents<JiggleRig>();
        rigs = rigs.Where(r => r != null).ToArray();
        if (tuning == null || tuning.Length != rigs.Length) CaptureTuning();
        stateHashes = statesWithoutBakedCloth.Select(Animator.StringToHash).ToArray();
        Apply(0f);
    }

    private void CaptureTuning()
    {
        var source = rigs == null || rigs.Length == 0 ? GetComponents<JiggleRig>() : rigs.Where(r => r != null).ToArray();
        tuning = source.Select(r =>
        {
            var p = r.GetInputParameters();
            return new Tuning { stiffness = p.stiffness.value, soften = p.soften, stretch = p.stretch.value, gravity = p.gravity.value };
        }).ToArray();
    }

    // A plain loop: this runs every frame.
    private bool IsUncovered(int stateHash)
    {
        for (int i = 0; i < stateHashes.Length; i++)
        {
            if (stateHashes[i] == stateHash) return true;
        }

        return false;
    }

    private void Apply(float blend)
    {
        Blend = blend;
        for (int i = 0; i < rigs.Length; i++)
        {
            var parameters = rigs[i].GetInputParameters();
            parameters.stiffness.value = Mathf.Lerp(1f, tuning[i].stiffness, blend);
            parameters.soften = tuning[i].soften * blend;
            parameters.stretch.value = tuning[i].stretch * blend;
            parameters.gravity.value = tuning[i].gravity * blend;
            rigs[i].SetInputParameters(parameters);
            rigs[i].UpdateParameters(); // a jobs sync point; only while the blend is changing
        }
    }
}
