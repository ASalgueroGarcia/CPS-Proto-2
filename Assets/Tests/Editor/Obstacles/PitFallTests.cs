using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class PitFallTests
{
    private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string PitPrefabPath = "Assets/_Game/Prefabs/Obstacles/Pit.prefab";
    private const string PlayerConfigAssetPath = "Assets/_Game/Data/Player/PlayerConfig.asset";

    private readonly List<Object> createdObjects = new List<Object>();

    private class TeleportProbe : MonoBehaviour, ITeleportListener
    {
        public int Calls;

        public void OnTeleported()
        {
            Calls++;
        }
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ShouldTriggerFall_OnlyTriggersWhileDashing_NotWalkingOrAlreadyFalling()
    {
        Bounds pitBounds = new Bounds(new Vector3(-10f, 0.75f, 2f), new Vector3(6f, 2f, 18f));
        Vector3 playerPos = new Vector3(-6.4f, 0.33f, 2f);
        Vector3 wallNormal = Vector3.right;
        Vector3 dashIntoPit = Vector3.left;

        Assert.IsFalse(PitFallRules.ShouldTriggerFall(false, false, dashIntoPit, wallNormal, playerPos, pitBounds));
        Assert.IsFalse(PitFallRules.ShouldTriggerFall(true, true, dashIntoPit, wallNormal, playerPos, pitBounds));
        Assert.IsTrue(PitFallRules.ShouldTriggerFall(true, false, dashIntoPit, wallNormal, playerPos, pitBounds));
    }

    [Test]
    public void ShouldTriggerFall_RequiresInwardDashComponent_IgnoresParallelOrOutwardDash()
    {
        Bounds pitBounds = new Bounds(new Vector3(-10f, 0.75f, 2f), new Vector3(6f, 2f, 18f));
        Vector3 playerPos = new Vector3(-6.4f, 0.33f, 2f);
        Vector3 wallNormal = Vector3.right;

        Assert.IsTrue(PitFallRules.ShouldTriggerFall(
            true, false, new Vector3(-1f, 0f, 1f).normalized, wallNormal, playerPos, pitBounds));

        Assert.IsFalse(PitFallRules.ShouldTriggerFall(
            true, false, Vector3.forward, wallNormal, playerPos, pitBounds));

        Assert.IsFalse(PitFallRules.ShouldTriggerFall(
            true, false, Vector3.right, wallNormal, playerPos, pitBounds));
    }

    [Test]
    public void ComputeSafeRespawnPosition_KeepsDistantSafeSpot_AndPushesEdgeSpotClear()
    {
        Bounds pitBounds = new Bounds(new Vector3(-10f, 0.75f, 2f), new Vector3(6f, 2f, 18f));

        Vector3 distantSafe = new Vector3(-3f, 0.33f, 2f);
        Assert.AreEqual(distantSafe, PitFallRules.ComputeSafeRespawnPosition(distantSafe, pitBounds, 0.9f));

        Vector3 edgeSpot = new Vector3(-6.5f, 0.33f, 2f);
        Vector3 pushed = PitFallRules.ComputeSafeRespawnPosition(edgeSpot, pitBounds, 0.9f);
        Assert.AreEqual(-6.1f, pushed.x, 0.001f);
        Assert.AreEqual(0.33f, pushed.y, 0.001f);
        Assert.AreEqual(2f, pushed.z, 0.001f);
    }

    [Test]
    public void ComputeFallDropTarget_AndEvaluateFallPosition_ArcIntoPitInteriorWithoutOvershooting()
    {
        Bounds narrowPit = new Bounds(new Vector3(0f, 0.75f, 0f), new Vector3(1f, 2f, 1f));
        Vector3 start = new Vector3(-0.9f, 0.33f, 0f);

        Vector3 target = PitFallRules.ComputeFallDropTarget(start, Vector3.right, narrowPit, 2.2f);
        Assert.Greater(target.x, narrowPit.min.x);
        Assert.Less(target.x, narrowPit.max.x);
        Assert.AreEqual(0.33f - 2.2f, target.y, 0.001f);

        Assert.AreEqual(start, PitFallRules.EvaluateFallPosition(start, target, 0f));
        Assert.AreEqual(target, PitFallRules.EvaluateFallPosition(start, target, 1f));

        Vector3 mid = PitFallRules.EvaluateFallPosition(start, target, 0.5f);
        float horizontalProgress = Mathf.InverseLerp(start.x, target.x, mid.x);
        float verticalProgress = Mathf.InverseLerp(start.y, target.y, mid.y);
        Assert.Greater(horizontalProgress, verticalProgress);
    }

    [Test]
    public void EvaluateBlinkAlpha_OscillatesBetweenMinAlphaAndOpaque_AndEndsOpaque()
    {
        Assert.AreEqual(0.35f, PitFallRules.EvaluateBlinkAlpha(0f, 1.2f, 0.12f, 0.35f), 0.001f);
        Assert.AreEqual(1f, PitFallRules.EvaluateBlinkAlpha(0.12f, 1.2f, 0.12f, 0.35f), 0.001f);
        Assert.AreEqual(0.35f, PitFallRules.EvaluateBlinkAlpha(0.24f, 1.2f, 0.12f, 0.35f), 0.001f);
        Assert.AreEqual(1f, PitFallRules.EvaluateBlinkAlpha(1.2f, 1.2f, 0.12f, 0.35f), 0.001f);
        Assert.AreEqual(1f, PitFallRules.EvaluateBlinkAlpha(2.0f, 1.2f, 0.12f, 0.35f), 0.001f);
    }

    [Test]
    public void PitPrefab_AndRoomLayouts_KeepSolidColliderNavMeshCarvingIgnoreRaycastAndPitMarker()
    {
        GameObject pitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PitPrefabPath);
        Assert.IsNotNull(pitPrefab, "Pit.prefab is missing");

        Pit pit = pitPrefab.GetComponent<Pit>();
        Assert.IsNotNull(pit, "Pit.prefab must carry Pit component");

        BoxCollider box = pitPrefab.GetComponent<BoxCollider>();
        Assert.IsNotNull(box, "Pit.prefab needs BoxCollider to block normal walking");
        Assert.IsTrue(box.enabled);
        Assert.IsFalse(box.isTrigger, "Pit BoxCollider must stay solid so normal walking is blocked");

        NavMeshObstacle obstacle = pitPrefab.GetComponent<NavMeshObstacle>();
        Assert.IsNotNull(obstacle, "Pit.prefab needs NavMeshObstacle so enemies path around pits");
        Assert.IsTrue(obstacle.enabled && obstacle.carving);

        Assert.AreEqual(LayerMask.NameToLayer("Ignore Raycast"), pitPrefab.layer,
            "Pit.prefab must stay on Ignore Raycast so straight projectiles pass over pits");

        for (int i = 1; i <= 10; i++)
        {
            string layoutPath = $"Assets/_Game/Rooms/Layout_{i:000}.prefab";
            GameObject layout = AssetDatabase.LoadAssetAtPath<GameObject>(layoutPath);
            Assert.IsNotNull(layout, $"{layoutPath} missing");
            Pit[] pits = layout.GetComponentsInChildren<Pit>(true);
            Assert.IsNotEmpty(pits, $"{layoutPath} should contain Pit instances");
        }

        PlayerConfig configAsset = AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigAssetPath);
        Assert.IsNotNull(configAsset);
        Assert.AreEqual(15f, configAsset.pitFallDamage, 0.001f);
    }

    [Test]
    public void DashingIntoPit_DropsLocksInputTeleportsAppliesFairDamageResetsListenersAndBlinks()
    {
        GameObject pitObject = new GameObject("Pit");
        createdObjects.Add(pitObject);
        pitObject.transform.position = new Vector3(-10f, -0.25f, 2f);
        pitObject.transform.localScale = new Vector3(6f, 1f, 18f);
        BoxCollider pitCollider = pitObject.AddComponent<BoxCollider>();
        pitCollider.center = new Vector3(0f, 1f, 0f);
        pitCollider.size = new Vector3(1f, 2f, 1f);
        Pit pit = pitObject.AddComponent<Pit>();

        GameObject playerObject = new GameObject("Player");
        createdObjects.Add(playerObject);
        CharacterController controller = playerObject.AddComponent<CharacterController>();
        Health health = playerObject.AddComponent<Health>();
        health.maxHealth = 100f;
        health.currentHealth = 100f;

        PlayerConfig config = ScriptableObject.CreateInstance<PlayerConfig>();
        createdObjects.Add(config);
        config.pitFallDamage = 15f;
        config.fallDuration = 0.35f;
        config.fallDepth = 2.2f;
        config.pitRespawnClearance = 0.9f;
        config.respawnBlinkDuration = 1.2f;
        config.respawnBlinkInterval = 0.12f;
        config.respawnMinAlpha = 0.35f;
        config.respawnInvulnerabilityDuration = 1.0f;

        Player player = playerObject.AddComponent<Player>();
        player.controller = controller;
        player.playerHealth = health;
        typeof(Player).GetField("config", NonPublicInstance).SetValue(player, config);
        typeof(Player).GetMethod("Awake", NonPublicInstance).Invoke(player, null);
        createdObjects.Add(player.Config);

        GameObject scarfChild = new GameObject("Scarf");
        scarfChild.transform.SetParent(playerObject.transform, false);
        TeleportProbe teleportProbe = scarfChild.AddComponent<TeleportProbe>();

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material sharedMat = new Material(shader);
        createdObjects.Add(sharedMat);
        MeshRenderer renderer = scarfChild.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = sharedMat;

        Vector3 safePosition = new Vector3(-3f, 0.33f, 2f);
        playerObject.transform.position = safePosition;
        player.PitFall.RecordSafePosition(safePosition);

        // Move to the pit edge and start a dash into the pit (which enables dash invulnerability).
        playerObject.transform.position = new Vector3(-6.5f, 0.33f, 2f);
        player.Dash.StartDash(Vector3.left);
        player.SwitchState(Player.PlayerState.Dashing);
        Assert.IsTrue(health.isInvulnerable, "Dash should set invulnerability");

        bool triggered = player.PitFall.TryTriggerPitFall(pit, Vector3.right);
        Assert.IsTrue(triggered);
        Assert.IsTrue(player.PitFall.IsFalling);
        Assert.IsTrue(player.InputLocked);
        Assert.IsFalse(controller.enabled);
        Assert.AreEqual(Player.PlayerState.Idle, player.currentState);

        // Mid-fall: dropping into the pit, no damage yet.
        player.PitFall.Tick(0.2f);
        Assert.IsTrue(player.PitFall.IsFalling);
        Assert.Less(playerObject.transform.position.y, 0.33f);
        Assert.AreEqual(100f, health.currentHealth, 0.001f);

        // Finish fall: teleports back to safe position, clears dash invuln, deals 15 damage, grants respawn invuln & blink.
        player.PitFall.Tick(0.2f);
        Assert.IsFalse(player.PitFall.IsFalling);
        Assert.IsFalse(player.InputLocked);
        Assert.IsTrue(controller.enabled);
        Assert.AreEqual(safePosition.x, playerObject.transform.position.x, 0.001f);
        Assert.AreEqual(safePosition.y, playerObject.transform.position.y, 0.001f);
        Assert.AreEqual(safePosition.z, playerObject.transform.position.z, 0.001f);
        Assert.AreEqual(1, teleportProbe.Calls, "Respawn teleport must notify ITeleportListener components");
        Assert.AreEqual(85f, health.currentHealth, 0.001f, "Pit respawn must deal 15 fair trap damage, not 100 instakill");
        Assert.IsTrue(health.isInvulnerable, "Respawn should grant brief invulnerability");
        Assert.IsTrue(player.PitFall.RespawnBlink.IsBlinking, "Respawn should start blink");

        // Respawn invulnerability expires cleanly after configured duration.
        player.PitFall.Tick(1.05f);
        Assert.IsFalse(health.isInvulnerable);
    }

    [Test]
    public void RespawnBlink_SwitchesUrpMaterialToTransparentOscillatesAlphaAndPreservesSharedMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material sharedMat = new Material(shader);
        createdObjects.Add(sharedMat);

        int baseColorId = Shader.PropertyToID("_BaseColor");
        int colorId = Shader.PropertyToID("_Color");
        int surfaceId = Shader.PropertyToID("_Surface");

        Color opaqueTint = new Color(0.86f, 0.83f, 0.78f, 1f);
        if (sharedMat.HasProperty(baseColorId)) sharedMat.SetColor(baseColorId, opaqueTint);
        if (sharedMat.HasProperty(colorId)) sharedMat.SetColor(colorId, opaqueTint);
        if (sharedMat.HasProperty(surfaceId)) sharedMat.SetFloat(surfaceId, 0f);
        sharedMat.renderQueue = (int)RenderQueue.Geometry;

        GameObject host = new GameObject("BlinkHost");
        createdObjects.Add(host);
        MeshRenderer meshRenderer = host.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = sharedMat;

        PlayerRespawnBlink blink = host.AddComponent<PlayerRespawnBlink>();
        blink.StartBlink(1.2f, 0.12f, 0.35f);

        Assert.IsTrue(blink.IsBlinking);
        Assert.AreNotSame(sharedMat, meshRenderer.sharedMaterial, "EditMode blink must not mutate shared asset");

        if (sharedMat.HasProperty(baseColorId))
            Assert.AreEqual(1f, sharedMat.GetColor(baseColorId).a, 0.001f);
        if (sharedMat.HasProperty(surfaceId))
            Assert.AreEqual(0f, sharedMat.GetFloat(surfaceId), 0.001f);

        Material activeMat = meshRenderer.sharedMaterial;
        Assert.AreEqual((int)RenderQueue.Transparent, activeMat.renderQueue);
        if (activeMat.HasProperty(surfaceId))
            Assert.AreEqual(1f, activeMat.GetFloat(surfaceId), 0.001f);

        int activeColorProp = activeMat.HasProperty(baseColorId) ? baseColorId : colorId;
        Assert.AreEqual(0.35f, activeMat.GetColor(activeColorProp).a, 0.001f);

        blink.TickBlink(0.12f);
        Assert.AreEqual(1f, activeMat.GetColor(activeColorProp).a, 0.001f);

        blink.TickBlink(0.12f);
        Assert.AreEqual(0.35f, activeMat.GetColor(activeColorProp).a, 0.001f);

        // Complete blink and verify full restoration.
        blink.TickBlink(1.0f);
        Assert.IsFalse(blink.IsBlinking);
        Assert.AreSame(sharedMat, meshRenderer.sharedMaterial);

        // Restart blink and verify stopping it restores immediately.
        blink.StartBlink(1.2f, 0.12f, 0.35f);
        Assert.IsTrue(blink.IsBlinking);
        blink.StopBlink();
        Assert.IsFalse(blink.IsBlinking);
        Assert.AreSame(sharedMat, meshRenderer.sharedMaterial);
    }
}
