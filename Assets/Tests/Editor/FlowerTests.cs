using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class FlowerTests
{
    private const string FlowerSettingsPath = "Assets/_Game/Data/Enemies/FlowerSettings.asset";
    private const string FlowerPrefabPath = "Assets/_Game/Prefabs/Enemies/Flower.prefab";
    private const string DeckPrefabPath = "Assets/_Game/Prefabs/Enemies/Deck_Of_Cards.prefab";

    private FlowerSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = ScriptableObject.CreateInstance<FlowerSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        if (settings != null) Object.DestroyImmediate(settings);
    }

    [Test]
    public void FlowerSettings_asset_loads_with_unified_gdd_defaults()
    {
        FlowerSettings asset = AssetDatabase.LoadAssetAtPath<FlowerSettings>(FlowerSettingsPath);
        Assert.That(asset, Is.Not.Null, $"Missing {FlowerSettingsPath}");
        Assert.That(asset.maxHealth, Is.EqualTo(10f));
        Assert.That(asset.biteRange, Is.EqualTo(1.5f));
        Assert.That(asset.biteDamage, Is.EqualTo(15f));
        Assert.That(asset.biteWindup, Is.EqualTo(0.5f));
        Assert.That(asset.biteCooldown, Is.EqualTo(1.5f));
        Assert.That(asset.projectileRange, Is.EqualTo(6f));
        Assert.That(asset.projectileDamage, Is.EqualTo(10f));
        Assert.That(asset.projectileSpeed, Is.EqualTo(5f));
        Assert.That(asset.projectileCooldown, Is.EqualTo(2f));
        Assert.That(asset.growDuration, Is.EqualTo(6f));
        Assert.That(asset.maxSpawnedFlowersAlive, Is.EqualTo(2));
        Assert.That(asset.burrowMinTier, Is.EqualTo(3));
        Assert.That(asset.burrowWindup, Is.EqualTo(1f));
        Assert.That(asset.burrowUndergroundTime, Is.EqualTo(2f));
        Assert.That(asset.burrowCooldown, Is.EqualTo(3f));
    }

    [Test]
    public void Tier2_bites_close_shoots_midrange_and_alternates_pollinating_after_growing()
    {
        Assert.That(
            FlowerDecisionLogic.EvaluateAction(2, 1.2f, false, false, true, true, true, false, false, 0, settings),
            Is.EqualTo(FlowerAction.Bite));

        Assert.That(
            FlowerDecisionLogic.EvaluateAction(2, 1.2f, false, false, false, true, true, false, false, 0, settings),
            Is.EqualTo(FlowerAction.Idle));

        Assert.That(
            FlowerDecisionLogic.EvaluateAction(2, 4.0f, false, false, true, true, true, false, true, 0, settings),
            Is.EqualTo(FlowerAction.NormalShot));

        Assert.That(
            FlowerDecisionLogic.EvaluateAction(2, 4.0f, false, false, true, true, true, true, true, 0, settings),
            Is.EqualTo(FlowerAction.PollinatingShot));

        Assert.That(
            FlowerDecisionLogic.EvaluateAction(2, 4.0f, false, false, true, true, true, true, true, 2, settings),
            Is.EqualTo(FlowerAction.NormalShot),
            "Pollinating falls back to NormalShot once 2 own Flowers are alive");

        Assert.That(
            FlowerDecisionLogic.EvaluateAction(2, 7.5f, true, false, true, true, true, true, true, 0, settings),
            Is.EqualTo(FlowerAction.Idle),
            "Tier 2 never burrows and does not shoot beyond 6 m");
    }

    [Test]
    public void Tier3_burrows_when_out_of_bite_range_and_aware_or_damaged()
    {
        Assert.That(
            FlowerDecisionLogic.EvaluateAction(3, 5.0f, false, false, true, true, true, false, false, 0, settings),
            Is.EqualTo(FlowerAction.Burrow));

        Assert.That(
            FlowerDecisionLogic.EvaluateAction(3, 9.0f, false, true, true, true, true, false, false, 0, settings),
            Is.EqualTo(FlowerAction.Burrow),
            "Taking damage wakes Tier 3 burrow even beyond 6 m");

        Assert.That(
            FlowerDecisionLogic.EvaluateAction(3, 4.0f, true, false, true, true, false, false, false, 0, settings),
            Is.EqualTo(FlowerAction.NormalShot),
            "When burrow is on cooldown, Tier 3 falls back to shooting within 6 m");

        Assert.That(
            FlowerDecisionLogic.EvaluateAction(3, 1.0f, true, false, true, true, true, false, false, 0, settings),
            Is.EqualTo(FlowerAction.Bite),
            "Bite always takes priority inside 1.5 m");
    }

    [Test]
    public void ClampBurrowTarget_and_CurvedSeedPosition_stay_within_bounds()
    {
        Vector3 clamped = FlowerDecisionLogic.ClampBurrowTarget(Vector3.zero, new Vector3(12f, 0f, 0f), 6f);
        Assert.That(clamped.x, Is.EqualTo(6f).Within(0.001f));
        Assert.That(clamped.z, Is.EqualTo(0f).Within(0.001f));

        Vector3 apex = FlowerDecisionLogic.EvaluateCurvedSeedPosition(Vector3.zero, new Vector3(4f, 0f, 0f), 2.5f, 0.5f);
        Assert.That(apex.x, Is.EqualTo(2f).Within(0.001f));
        Assert.That(apex.y, Is.EqualTo(2.5f).Within(0.001f));
    }

    [Test]
    public void Flower_and_DeckOfCards_prefabs_have_settings_spawner_and_launch_wired()
    {
        GameObject flowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlowerPrefabPath);
        Assert.That(flowerPrefab, Is.Not.Null);
        FlowerAttack attack = flowerPrefab.GetComponent<FlowerAttack>();
        Health flowerHealth = flowerPrefab.GetComponent<Health>();
        Assert.That(attack, Is.Not.Null);
        Assert.That(attack, Is.InstanceOf<IEnemySpawner>());
        Assert.That(attack.Settings, Is.Not.Null);
        Assert.That(flowerHealth.maxHealth, Is.EqualTo(10f));

        GameObject deckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DeckPrefabPath);
        Assert.That(deckPrefab, Is.Not.Null);
        SerializedObject deckEnemy = new SerializedObject(deckPrefab.GetComponent<Enemy>());
        Assert.That(deckEnemy.FindProperty("launchSettings").objectReferenceValue, Is.Not.Null);
    }
}
