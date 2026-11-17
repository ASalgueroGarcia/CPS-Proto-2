using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// OnTriggerStay fires every physics step, so the spikes have to limit how often one body gets hit.
public class FloorSpikesTests
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private readonly List<GameObject> _created = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in _created) Object.DestroyImmediate(go);
        _created.Clear();
    }

    private FloorSpikes NewActiveSpikes()
    {
        var go = new GameObject("Spikes");
        _created.Add(go);
        FloorSpikes spikes = go.AddComponent<FloorSpikes>();
        typeof(FloorSpikes).GetField("_isA", Any).SetValue(spikes, true);
        return spikes;
    }

    private Collider NewBody(out Health health)
    {
        var go = new GameObject("Body") { tag = "Player" };
        _created.Add(go);
        health = go.AddComponent<Health>();
        health.maxHealth = 400f;
        health.currentHealth = 400f;
        return go.AddComponent<BoxCollider>();
    }

    private static void Stay(FloorSpikes spikes, Collider other)
    {
        typeof(FloorSpikes).GetMethod("OnTriggerStay", Any).Invoke(spikes, new object[] { other });
    }

    [Test]
    public void StandingOnThem_HitsOnce_NotEveryPhysicsStep()
    {
        FloorSpikes spikes = NewActiveSpikes();
        Collider body = NewBody(out Health health);
        for (int i = 0; i < 10; i++) Stay(spikes, body);
        Assert.AreEqual(385f, health.currentHealth);
    }

    [Test]
    public void TheIntervalIsPerBody()
    {
        FloorSpikes spikes = NewActiveSpikes();
        Collider first = NewBody(out Health a);
        Collider second = NewBody(out Health b);
        Stay(spikes, first);
        Stay(spikes, second);
        Assert.AreEqual(385f, a.currentHealth);
        Assert.AreEqual(385f, b.currentHealth);
    }

    [Test]
    public void InactiveSpikes_DoNothing()
    {
        FloorSpikes spikes = NewActiveSpikes();
        typeof(FloorSpikes).GetField("_isA", Any).SetValue(spikes, false);
        Collider body = NewBody(out Health health);
        Stay(spikes, body);
        Assert.AreEqual(400f, health.currentHealth);
    }
}
