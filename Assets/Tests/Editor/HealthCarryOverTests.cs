using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// GDD (Fight Nodes & Rooms): "HP carries over from room to room. It's never refilled automatically."
/// Each room spawns a new player; PlayerStatsManager.BindToPlayer gives it the run's HP with
/// RestoreHealth before its Start runs, and Start must not refill it.
/// </summary>
public class HealthCarryOverTests
{
    private readonly List<GameObject> _created = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _created) Object.DestroyImmediate(go);
        _created.Clear();
    }

    private Health NewBody()
    {
        var go = new GameObject("TestBody");
        _created.Add(go);
        return go.AddComponent<Health>();
    }

    private static void RunStart(Health health)
    {
        MethodInfo start = typeof(Health).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(start, "Health has no Start() - this test needs updating.");
        start.Invoke(health, null);
    }

    [Test]
    public void FreshBody_StartsFull()
    {
        Health health = NewBody();
        typeof(Health).GetField("_currentHealth", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(health, 40f);

        RunStart(health);

        Assert.AreEqual(100f, health.currentHealth, "A body nobody restored (enemies, a new run) starts at max.");
    }

    [Test]
    public void RestoredHealth_SurvivesStart()
    {
        Health health = NewBody();
        float reported = -1f;
        health.OnHealthChanged = new UnityEngine.Events.UnityEvent<float, float>();
        health.OnHealthChanged.AddListener((current, max) => reported = current);

        health.RestoreHealth(37f, 100f);
        RunStart(health);

        Assert.AreEqual(37f, health.currentHealth, "The run's HP given before Start must not be refilled.");
        Assert.AreEqual(37f, reported, "Start still reports the HP, so the HUD shows it.");
    }

    [Test]
    public void RestoredHealth_KeepsARaisedMax()
    {
        Health health = NewBody();

        health.RestoreHealth(90f, 120f);
        RunStart(health);

        Assert.AreEqual(120f, health.maxHealth);
        Assert.AreEqual(90f, health.currentHealth);
    }

    [Test]
    public void PlainSetBeforeStart_StillStartsFull()
    {
        // Player.Awake / Enemy.Awake set health from their data before Start; that isn't a restore.
        Health health = NewBody();
        health.currentHealth = 30f;

        RunStart(health);

        Assert.AreEqual(100f, health.currentHealth);
    }

    [Test]
    public void AfterStart_HealthChangesAsBefore()
    {
        Health health = NewBody();
        RunStart(health);

        health.currentHealth = 55f;
        Assert.AreEqual(55f, health.currentHealth);
        health.ResetHealth();
        Assert.AreEqual(100f, health.currentHealth);
    }
}
