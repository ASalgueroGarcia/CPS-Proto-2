using System;
using NUnit.Framework;
using UnityEngine;

public class CameraStopsTests
{
    // --- HubEntryFlow: the flight runs before the hub, nothing else waits for it ---

    [Test]
    public void EnterHub_WaitsForTheFlight_ThenEntersOnce()
    {
        var inner = new CountingFlow();
        var flight = new ManualFlight();
        var flow = new HubEntryFlow(inner, flight);

        flow.EnterHub();
        Assert.AreEqual(0, inner.HubEntered, "the hub must not load while the camera is still flying");

        flow.EnterHub(); // a second request mid-flight is ignored
        Assert.AreEqual(1, flight.Plays);

        flight.Finish();
        Assert.AreEqual(1, inner.HubEntered);
    }

    [Test]
    public void EnterHub_WithoutAFlight_EntersStraightAway()
    {
        var inner = new CountingFlow();
        new HubEntryFlow(inner, null).EnterHub();
        Assert.AreEqual(1, inner.HubEntered);
    }

    [Test]
    public void MainMenuAndQuit_PassThrough()
    {
        var inner = new CountingFlow();
        var flight = new ManualFlight();
        var flow = new HubEntryFlow(inner, flight);

        flow.EnterMainMenu();
        flow.Quit();

        Assert.AreEqual(1, inner.MainMenus);
        Assert.AreEqual(1, inner.Quits);
        Assert.AreEqual(0, flight.Plays);
    }

    // --- Turning between two camera points ---

    [Test]
    public void Turn_StartsAndEndsOnTheTwoFacings()
    {
        Quaternion from = Quaternion.LookRotation(Vector3.forward);
        Quaternion to = Quaternion.LookRotation(new Vector3(-1f, -0.5f, 0.2f));
        Assert.Less(Quaternion.Angle(from, CameraStops.Turn(from, to, 0f)), 0.01f);
        Assert.Less(Quaternion.Angle(to, CameraStops.Turn(from, to, 1f)), 0.01f);
    }

    [Test]
    public void Turn_NeverRollsTheHorizon()
    {
        // a side-on view at eye level turning into a steep look down from behind
        Quaternion from = Quaternion.LookRotation(Vector3.forward);
        Quaternion to = Quaternion.LookRotation(new Vector3(-1f, -0.8f, 0.3f));
        for (int i = 0; i <= 50; i++)
        {
            Vector3 right = CameraStops.Turn(from, to, i / 50f) * Vector3.right;
            Assert.Less(Mathf.Abs(right.y), 0.001f, $"horizon tilted at step {i}");
        }
    }

    [Test]
    public void Level_RemovesRollFromAPlacedPoint()
    {
        // a point rotated by hand with some roll still films a flat horizon
        Quaternion rolled = Quaternion.Euler(20f, 40f, 15f);
        Quaternion level = CameraStops.Level(rolled);

        Assert.Less(Mathf.Abs((level * Vector3.right).y), 0.001f);
        Assert.Less(Vector3.Angle(rolled * Vector3.forward, level * Vector3.forward), 0.01f, "the facing must not change");
    }

    // --- Parts that get out of the way ---

    [Test]
    public void Part_BetweenCameraAndFocus_Blocks()
    {
        var wall = new Bounds(new Vector3(0, 0, 5), new Vector3(4, 4, 0.2f));
        Assert.IsTrue(CameraStops.BlocksView(wall, Vector3.zero, new Vector3(0, 0, 10), 0f));
    }

    [Test]
    public void Part_AroundTheCamera_Blocks()
    {
        var box = new Bounds(Vector3.zero, Vector3.one * 2f);
        Assert.IsTrue(CameraStops.BlocksView(box, new Vector3(0.5f, 0, 0), new Vector3(0, 0, 10), 0f));
    }

    [Test]
    public void Part_BesideOrBehindOrPastTheFocus_DoesNotBlock()
    {
        Vector3 cam = Vector3.zero, focus = new Vector3(0, 0, 10);
        var beside = new Bounds(new Vector3(5, 0, 5), Vector3.one);
        var behind = new Bounds(new Vector3(0, 0, -5), Vector3.one);
        var pastFocus = new Bounds(new Vector3(0, 0, 15), Vector3.one);

        Assert.IsFalse(CameraStops.BlocksView(beside, cam, focus, 0f));
        Assert.IsFalse(CameraStops.BlocksView(behind, cam, focus, 0f));
        Assert.IsFalse(CameraStops.BlocksView(pastFocus, cam, focus, 0f));
    }

    [Test]
    public void Padding_WidensThePart()
    {
        var nearMiss = new Bounds(new Vector3(0.7f, 0, 5), Vector3.one); // edge at x = 0.2
        Assert.IsFalse(CameraStops.BlocksView(nearMiss, Vector3.zero, new Vector3(0, 0, 10), 0f));
        Assert.IsTrue(CameraStops.BlocksView(nearMiss, Vector3.zero, new Vector3(0, 0, 10), 0.3f));
    }

    private class ManualFlight : ICameraFlight
    {
        private Action _onFinished;
        public int Plays { get; private set; }

        public void Play(Action onFinished)
        {
            Plays++;
            _onFinished = onFinished;
        }

        public void Finish() => _onFinished?.Invoke();
    }

    private class CountingFlow : IGameFlow
    {
        public int HubEntered { get; private set; }
        public int MainMenus { get; private set; }
        public int Quits { get; private set; }

        public void EnterHub() => HubEntered++;
        public void EnterMainMenu() => MainMenus++;
        public void Quit() => Quits++;
    }
}
