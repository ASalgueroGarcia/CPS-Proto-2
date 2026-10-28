using System.Collections.Generic;
using System.Linq;
using GatorDragonGames.JigglePhysics;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guards Sarah's costume setup against a re-export from art or a stray edit: the scarf must stay its own mesh with
/// its bones, no clip may key the scarf (Cloth drives it from the FBX's draped pose), and the sandbox scene must keep
/// the Cloth on the scarf with its knot pinned and the Jiggle rigs on the robe.
/// </summary>
public class SarahCostumeAssetTests
{
    private const string FbxPath = "Assets/_Sandbox/Dima/CostumePhysics/Models/Sarah.fbx";
    private const string ScenePath = "Assets/_Sandbox/Dima/CostumePhysics/SarahCostume.unity";

    private static readonly string[] ScarfBones = { "Scarf_Start", "Scarf_01", "Scarf_02", "Scarf_03", "Scarf_04", "Scarf_05", "Scarf_06" };
    private static readonly string[] Clips =
    {
        "Idle", "Idle_Scissors", "Run", "Run_Scissors", "Dash", "PickUp", "Scissor_Attack_01", "Scissor_Attack_02",
        "Scissor_Attack_03", "Scissor_Attack_Strong", "TakeDamage_01", "TakeDamage_02", "Dying", "WakeUp_03",
    };

    private Scene scene;
    private bool openedByTest;

    [TearDown]
    public void TearDown()
    {
        // Only close what the test opened: closing a scene the user has open would drop their unsaved edits.
        if (openedByTest && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        openedByTest = false;
    }

    [Test]
    public void Model_HasScarfAsItsOwnSkinnedMesh()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        Assert.IsNotNull(model, FbxPath);
        var bones = model.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToList();
        foreach (var bone in ScarfBones) CollectionAssert.Contains(bones, bone);

        var scarf = model.transform.Find("Scarf_Geo")?.GetComponent<SkinnedMeshRenderer>();
        Assert.IsNotNull(scarf, "Scarf_Geo is missing or not skinned");
        // The export lists every bone of the rig; what matters is which bones the scarf is weighted to.
        var used = scarf.sharedMesh.boneWeights
            .SelectMany(w => new[] { (w.boneIndex0, w.weight0), (w.boneIndex1, w.weight1), (w.boneIndex2, w.weight2), (w.boneIndex3, w.weight3) })
            .Where(w => w.Item2 > 0f).Select(w => scarf.bones[w.Item1].name).Distinct().ToList();
        var outside = used.Where(n => !n.StartsWith("Scarf") && !n.StartsWith("Cloth_03")).ToList();
        Assert.IsEmpty(outside, "Scarf_Geo is weighted to bones outside the scarf");
        Assert.IsNotNull(model.transform.Find("MainCharacter_Geo")?.GetComponent<SkinnedMeshRenderer>());
    }

    [Test]
    public void Clips_AllPresent_AndNoneKeysTheScarf()
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
        var duplicates = all.GroupBy(c => c.name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.IsEmpty(duplicates, "duplicate clip names");
        var clips = all.ToDictionary(c => c.name);
        foreach (var name in Clips)
        {
            Assert.IsTrue(clips.TryGetValue(name, out var clip), $"clip {name} missing");
            var scarfCurves = AnimationUtility.GetCurveBindings(clip).Where(b => b.path.Split('/').Last().StartsWith("Scarf")).ToList();
            Assert.IsEmpty(scarfCurves, $"{name} keys the scarf: {string.Join(", ", scarfCurves.Select(b => b.path))}");
        }
    }

    [Test]
    public void Scene_ScarfHasCloth_WithKnotPinnedAndTailFree()
    {
        var sarah = OpenSarah();
        var scarf = sarah.transform.Find("Scarf_Geo");
        var cloth = scarf.GetComponent<Cloth>();
        Assert.IsNotNull(cloth, "no Cloth on Scarf_Geo");
        Assert.IsTrue(cloth.enabled, "the scarf's Cloth is disabled");
        Assert.IsTrue(scarf.GetComponents<MonoBehaviour>().Any(m => m.GetType().Name == "ClothEnableReset" && m.enabled), "ClothEnableReset missing or disabled");
        Assert.IsTrue(cloth.capsuleColliders.Length > 0 && cloth.capsuleColliders.All(c => c != null), "scarf colliders missing");

        var skinned = scarf.GetComponent<SkinnedMeshRenderer>();
        var mesh = skinned.sharedMesh;
        var welded = new Dictionary<Vector3, int>();
        foreach (var v in mesh.vertices) if (!welded.ContainsKey(v)) welded[v] = welded.Count;
        var coefficients = cloth.coefficients;
        Assert.AreEqual(welded.Count, coefficients.Length, "cloth coefficients don't match the mesh");

        // The knot (weighted only to Scarf_Start) must be pinned; the tail must be free to move.
        var weights = mesh.boneWeights;
        var vertices = mesh.vertices;
        int knot = System.Array.FindIndex(skinned.bones, b => b.name == "Scarf_Start");
        var seen = new HashSet<Vector3>();
        int knotCount = 0, freeCount = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (!seen.Add(vertices[i])) continue;
            float maxDistance = coefficients[welded[vertices[i]]].maxDistance;
            if (weights[i].boneIndex0 == knot && weights[i].weight0 > 0.999f)
            {
                knotCount++;
                Assert.AreEqual(0f, maxDistance, $"knot vertex {i} is not pinned");
            }
            else if (maxDistance > 0f) freeCount++;
        }
        Assert.Greater(knotCount, 0, "no knot vertices found");
        Assert.Greater(freeCount, 0, "the whole scarf is pinned");
    }

    [Test]
    public void Scene_RobeRigs_HaveRootsAndDoNotNest()
    {
        var sarah = OpenSarah();
        var rigs = sarah.GetComponents<JiggleRig>();
        Assert.IsNotEmpty(rigs, "no Jiggle rigs on Sarah_Hybrid");
        var damageBlend = sarah.GetComponent<JiggleDamageBlend>();
        Assert.IsTrue(damageBlend != null && damageBlend.enabled, "JiggleDamageBlend missing or disabled: the robe would double the baked motion");
        var listed = new SerializedObject(damageBlend).FindProperty("rigs");
        var listedRigs = Enumerable.Range(0, listed.arraySize).Select(i => listed.GetArrayElementAtIndex(i).objectReferenceValue).ToList();
        Assert.IsTrue(rigs.All(r => listedRigs.Contains(r)), "a JiggleRig isn't faded by JiggleDamageBlend: it would jiggle on top of the baked motion");
        var roots = rigs.Select(r => r.GetJiggleRigData().rootBone).ToList();
        Assert.IsTrue(roots.All(r => r != null), "a Jiggle rig has no root bone");
        foreach (var rig in rigs)
        {
            var data = rig.GetJiggleRigData();
            Assert.IsTrue(rig.enabled, $"rig {data.rootBone.name} is disabled");
            Assert.IsTrue(data.excludedTransforms.All(t => t != null), $"rig {data.rootBone.name} has a missing excluded bone");
            Assert.IsNotEmpty(data.jiggleColliders, $"rig {data.rootBone.name} has no colliders");
            Assert.IsTrue(data.jiggleColliders.All(c => c.transform != null), $"rig {data.rootBone.name} has a missing collider");
            var cached = data.transformCachedData.Select(c => c.bone).ToList();
            Assert.IsNotEmpty(cached, $"rig {data.rootBone.name} has no cached bones");
            // A rig whose tree contains another rig's root merges with it and drops that rig's settings.
            // (Contains, not CollectionAssert: NUnit would compare Transforms as collections of their children.)
            foreach (var other in roots.Where(r => r != data.rootBone)) Assert.IsFalse(cached.Contains(other), $"rig {data.rootBone.name} contains rig root {other.name}");
            Assert.IsFalse(cached.Any(b => b.name.StartsWith("Scarf")), $"rig {data.rootBone.name} jiggles the scarf, which belongs to Cloth");
            // At runtime the package walks the live hierarchy (not the cache): no collider object may be reachable.
            var reachable = new List<Transform>();
            var stack = new Stack<Transform>(new[] { data.rootBone });
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                reachable.Add(t);
                foreach (Transform child in t) if (!data.excludedTransforms.Contains(child)) stack.Push(child);
            }
            var colliders = reachable.Where(t => t.name.StartsWith("ClothCol_") || t.name.StartsWith("JiggleCol_")).Select(t => t.name).ToList();
            Assert.IsEmpty(colliders, $"rig {data.rootBone.name} would simulate collider objects as bones");
        }
    }

    [Test]
    public void DamageBlend_ZeroOutsideTakeDamage_FadesInAndOutOnTheRealController()
    {
        // A copy, so the test never edits the scene's own Sarah.
        var sarah = Object.Instantiate(OpenSarah());
        try
        {
            var animator = sarah.GetComponent<Animator>();
            var blend = sarah.GetComponent<JiggleDamageBlend>();
            var rigs = sarah.GetComponents<JiggleRig>();
            float tunedStiffness = rigs[0].GetInputParameters().stiffness.value;
            float target = blend.TargetBlend;
            Assert.Greater(target, 0f);
            animator.Rebind();

            // Every state with baked cloth motion: the switch stays off and the rigs hold the animated pose.
            var states = ((UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController).layers[0].stateMachine.states.Select(s => s.state.name).ToList();
            foreach (var state in states.Where(s => !s.StartsWith("TakeDamage")))
            {
                animator.Play(state, 0, 0f);
                animator.Update(0f);
                Assert.IsFalse(blend.InStateWithoutBakedCloth(), state);
                blend.Tick(blend.InStateWithoutBakedCloth(), 1f);
                Assert.AreEqual(0f, blend.Blend, state);
            }
            Assert.IsTrue(rigs.All(r => r.GetInputParameters().stiffness.value == 1f && r.GetInputParameters().soften == 0f
                                        && r.GetInputParameters().gravity.value == 0f), "rigs not pinned to the animated pose");

            // A crossfade counts from its start: into TakeDamage the switch is on, out of it the switch is off.
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
            animator.CrossFade("TakeDamage_01", 0.2f, 0, 0f);
            animator.Update(0.05f);
            Assert.IsTrue(animator.IsInTransition(0), "no crossfade to test");
            Assert.IsTrue(blend.InStateWithoutBakedCloth(), "crossfading into TakeDamage_01");
            animator.Update(0.3f);
            animator.CrossFade("Idle", 0.2f, 0, 0f);
            animator.Update(0.05f);
            Assert.IsTrue(animator.IsInTransition(0), "no crossfade out to test");
            Assert.IsFalse(blend.InStateWithoutBakedCloth(), "crossfading out of TakeDamage_01");

            foreach (var damage in new[] { "TakeDamage_01", "TakeDamage_02" })
            {
                animator.Play(damage, 0, 0f);
                animator.Update(0f);
                Assert.IsTrue(blend.InStateWithoutBakedCloth(), damage);
                blend.Tick(true, 0.05f);
                Assert.AreEqual(target * 0.5f, blend.Blend, 1e-4f, "halfway after half the fade");
                blend.Tick(true, 0.05f);
                Assert.AreEqual(target, blend.Blend, 1e-4f, "at the target after the fade");
                blend.Tick(true, 1f);
                Assert.AreEqual(target, blend.Blend, 1e-4f, "holds the target");
                Assert.AreEqual(Mathf.Lerp(1f, tunedStiffness, target), rigs[0].GetInputParameters().stiffness.value, 1e-4f);

                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
                Assert.IsFalse(blend.InStateWithoutBakedCloth());
                blend.Tick(false, 0.05f);
                Assert.AreEqual(target * 0.5f, blend.Blend, 1e-4f, "halfway out");
                blend.Tick(false, 0.06f);
                Assert.AreEqual(0f, blend.Blend, "back to 0 after the fade");
                Assert.AreEqual(1f, rigs[0].GetInputParameters().stiffness.value);
            }
        }
        finally
        {
            Object.DestroyImmediate(sarah);
        }
    }

    private GameObject OpenSarah()
    {
        scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedByTest = true;
        }
        var sarah = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Sarah_Hybrid");
        Assert.IsNotNull(sarah, "Sarah_Hybrid missing from " + ScenePath);
        return sarah;
    }
}
