using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Sarah (the main character) on Player.prefab: the model, its clips, the animator states the player code drives,
// and the Cloth scarf. The robe's motion is baked into the clips, so nothing else simulates it.
public class PlayerCostumeTests
{
    private const string Fbx = "Assets/_Game/Art/Characters/Player/Sarah.fbx";
    private const string ControllerPath = "Assets/_Game/Art/Characters/Player/CharacterController.controller";
    private const string PrefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";

    private static readonly string[] Clips =
    {
        "Idle", "Idle_Scissors", "Run", "Run_Scissors", "Dash", "PickUp", "Scissor_Attack_01", "Scissor_Attack_02",
        "Scissor_Attack_03", "Scissor_Attack_Strong", "TakeDamage_01", "TakeDamage_02", "Dying", "WakeUp_03",
    };

    // State -> clip. The state names are the ones PlayerLocomotion/PlayerDashController/ScissorsWeapon drive.
    private static readonly Dictionary<string, string> CodeStates = new Dictionary<string, string>
    {
        { "Idle", "Idle_Scissors" }, { "Run", "Run_Scissors" }, { "Dash", "Dash" }, { "Attack_01", "Scissor_Attack_01" },
        { "Attack_02", "Scissor_Attack_02" }, { "Attack_03", "Scissor_Attack_03" }, { "HeavyAttack", "Scissor_Attack_Strong" },
    };

    private static Dictionary<string, AnimationClip> SarahClips() =>
        AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);

    [Test]
    public void Sarah_ScarfIsItsOwnMesh_AndNoClipKeysIt()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        var scarf = model.transform.Find("Scarf_Geo")?.GetComponent<SkinnedMeshRenderer>();
        Assert.IsNotNull(scarf, "Sarah.fbx has no Scarf_Geo skinned mesh");
        Assert.IsTrue(scarf.sharedMesh.isReadable, "Cloth needs the scarf mesh readable (Read/Write on Sarah.fbx)");

        var clips = SarahClips();
        CollectionAssert.AreEquivalent(Clips, clips.Keys, "Sarah's clips changed");
        foreach (var clip in clips.Values)
        {
            var scarfCurves = AnimationUtility.GetCurveBindings(clip).Where(b => b.path.Split('/').Last().StartsWith("Scarf")).ToList();
            Assert.IsEmpty(scarfCurves, $"{clip.name} keys the scarf, which fights the Cloth (use Sarah_NoScarf.mask)");
        }
    }

    [Test]
    public void Sarah_AttackClipsSendTheEventsTheWeaponNeeds()
    {
        var clips = SarahClips();
        var expected = new Dictionary<string, string[]>
        {
            { "Scissor_Attack_01", new[] { "EnableHitbox12", "OpenComboWindow", "DisableHitbox", "ReturnToIdle" } },
            { "Scissor_Attack_02", new[] { "EnableHitbox12", "OpenComboWindow", "DisableHitbox", "ReturnToIdle" } },
            { "Scissor_Attack_03", new[] { "EnableHitbox3", "OpenComboWindow", "DisableHitbox", "ReturnToIdle" } },
            { "Scissor_Attack_Strong", new[] { "ExecuteSpecialAttackDamage", "ReturnToIdle" } },
        };
        foreach (var pair in expected)
        {
            var events = clips[pair.Key].events.Select(e => e.functionName).ToList();
            CollectionAssert.AreEqual(pair.Value, events, $"{pair.Key} events");
            foreach (string function in events)
                Assert.IsNotNull(typeof(AnimationEventForwarder).GetMethod(function), $"{pair.Key}: AnimationEventForwarder has no {function}()");
        }
    }

    [Test]
    public void Controller_KeepsTheStatesAndParametersTheCodeUses_AndPlaysSarahsClips()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var states = controller.layers[0].stateMachine.states.ToDictionary(s => s.state.name, s => s.state);
        foreach (var pair in CodeStates)
        {
            Assert.IsTrue(states.ContainsKey(pair.Key), $"state {pair.Key} is gone: the player code plays it by name");
            var clip = states[pair.Key].motion as AnimationClip;
            Assert.IsNotNull(clip, $"state {pair.Key} has no clip");
            Assert.AreEqual(Fbx, AssetDatabase.GetAssetPath(clip), $"state {pair.Key} still plays {clip.name} from {AssetDatabase.GetAssetPath(clip)}");
            Assert.AreEqual(pair.Value, clip.name, $"state {pair.Key}");
        }
        var parameters = controller.parameters.ToDictionary(p => p.name, p => p.type);
        Assert.AreEqual(AnimatorControllerParameterType.Bool, parameters["IsMoving"]);
        Assert.AreEqual(AnimatorControllerParameterType.Bool, parameters["IsDashing"]);
        Assert.AreEqual(AnimatorControllerParameterType.Trigger, parameters["HeavyAttack"]);
        Assert.IsTrue(states.Values.All(s => s.motion == null || AssetDatabase.GetAssetPath(s.motion) == Fbx), "a state still plays a Main_Char clip");
    }

    [Test]
    public void PlayerPrefab_IsSarah_WithAClothScarfAndNoJiggle()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var player = prefab.GetComponent<Player>();
        var animator = player.animator;
        Assert.IsNotNull(animator, "Player.animator is not set");
        Assert.AreEqual(Fbx, AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(animator.gameObject)), "Player.animator is not on the Sarah model");
        Assert.AreEqual(ControllerPath, AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
        Assert.IsNotNull(animator.GetComponent<AnimationEventForwarder>(), "attack events need an AnimationEventForwarder next to the Animator");
        Assert.IsNotNull(animator.GetComponent<ModelTurnSmoother>(), "without the turn smoother an instant 180° whips the scarf through the body");
        Assert.AreEqual("MainCharacter_Geo", player.bodyRenderer != null ? player.bodyRenderer.name : null, "Player.bodyRenderer (the combo tint) is not Sarah's body");
        Assert.IsTrue(animator.GetComponentsInChildren<Renderer>(true).All(r => r.sharedMaterials.All(m => m != null)), "a Sarah renderer has no material");
        Assert.IsFalse(prefab.GetComponentsInChildren<MonoBehaviour>(true).Any(m => m != null && m.GetType().Namespace == "GatorDragonGames.JigglePhysics"),
            "a JigglePhysics component is on the player: the robe's motion is baked into the clips");

        var cloths = prefab.GetComponentsInChildren<Cloth>(true);
        Assert.AreEqual(1, cloths.Length, "exactly one Cloth (the scarf)");
        var cloth = cloths[0];
        Assert.AreEqual("Scarf_Geo", cloth.name);
        Assert.IsTrue(cloth.enabled && cloth.GetComponent<ClothReset>() != null && cloth.GetComponent<ClothReset>().enabled, "Cloth or ClothReset disabled/missing");
        Assert.IsNotEmpty(cloth.capsuleColliders, "the scarf has no body colliders");
        foreach (var capsule in cloth.capsuleColliders)
        {
            Assert.IsNotNull(capsule, "a missing scarf collider");
            // Disabled: Cloth still collides with them, but physics doesn't, so they don't block the CharacterController
            // or get hit by explosions/traps (which would damage the player once per collider).
            Assert.IsFalse(capsule.enabled, $"{capsule.name} is enabled: it would collide with the player's CharacterController and with hit queries");
        }
    }

    [Test]
    public void PlayerPrefab_ScarfKnotIsPinned_TailIsFree()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var cloth = prefab.GetComponentInChildren<Cloth>(true);
        var skinned = cloth.GetComponent<SkinnedMeshRenderer>();
        var mesh = skinned.sharedMesh;
        var vertices = mesh.vertices;
        var welded = new Dictionary<Vector3, int>();
        foreach (var v in vertices) if (!welded.ContainsKey(v)) welded[v] = welded.Count;
        var coefficients = cloth.coefficients;
        Assert.AreEqual(welded.Count, coefficients.Length, "cloth coefficients don't match the scarf mesh");

        int knot = System.Array.FindIndex(skinned.bones, b => b.name == "Scarf_Start");
        var weights = mesh.boneWeights;
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
            // The tail (mostly on the last scarf bones) must swing well clear of the body, not barely move.
            string tailBone = skinned.bones[weights[i].boneIndex0].name;
            if ((tailBone == "Scarf_05" || tailBone == "Scarf_06") && weights[i].weight0 > 0.5f)
                Assert.Greater(maxDistance * skinned.rootBone.lossyScale.x, 0.25f, $"tail vertex {i} ({tailBone}) can move only {maxDistance * skinned.rootBone.lossyScale.x:0.000} m");
        }
        Assert.Greater(knotCount, 0, "no knot vertices found");
        Assert.Greater(freeCount, 0, "the whole scarf is pinned");
    }

    private class TeleportProbe : MonoBehaviour, ITeleportListener
    {
        public int Calls;
        public void OnTeleported() => Calls++;
    }

    [Test]
    public void DoorTeleport_TellsTheListenersUnderThePlayer_IncludingTheScarf()
    {
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        var door = new GameObject("Door").AddComponent<RoomDoorTransition>();
        try
        {
            Assert.IsTrue(player.GetComponentsInChildren<ITeleportListener>(true).OfType<ClothReset>().Any(r => r.GetComponent<Cloth>() != null),
                "the scarf's Cloth doesn't listen for teleports");
            var probe = player.GetComponentInChildren<Cloth>(true).gameObject.AddComponent<TeleportProbe>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(RoomDoorTransition).GetField("player", flags).SetValue(door, player.GetComponent<Player>());
            typeof(RoomDoorTransition).GetMethod("Teleport", flags).Invoke(door, new object[] { new Vector3(3f, 0f, 4f) });
            Assert.AreEqual(1, probe.Calls, "RoomDoorTransition.Teleport didn't tell the listeners under the player");
        }
        finally
        {
            Object.DestroyImmediate(door.gameObject);
            Object.DestroyImmediate(player);
        }
    }
}
