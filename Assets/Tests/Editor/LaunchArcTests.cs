using NUnit.Framework;
using UnityEngine;

/// <summary>The arc and bounce direction of an enemy launched off a wall.</summary>
public class LaunchArcTests
{
    private static readonly Vector3 Start = new Vector3(1f, 0f, 2f);
    private static readonly Vector3 End = new Vector3(5f, 0f, 2f);

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.That(Vector3.Distance(expected, actual), Is.LessThan(0.0001f), $"expected {expected}, got {actual}");

    [Test]
    public void Arc_starts_and_ends_on_the_ground_points()
    {
        AssertClose(Start, LaunchArc.Evaluate(Start, End, 1.5f, 0f));
        AssertClose(End, LaunchArc.Evaluate(Start, End, 1.5f, 1f));
    }

    [Test]
    public void Arc_peaks_at_the_given_height_halfway()
    {
        AssertClose(new Vector3(3f, 1.5f, 2f), LaunchArc.Evaluate(Start, End, 1.5f, 0.5f));
    }

    [Test]
    public void Arc_clamps_time_outside_zero_to_one()
    {
        AssertClose(Start, LaunchArc.Evaluate(Start, End, 1.5f, -1f));
        AssertClose(End, LaunchArc.Evaluate(Start, End, 1.5f, 2f));
    }

    [Test]
    public void Head_on_hit_bounces_straight_back()
    {
        AssertClose(Vector3.back, LaunchArc.BounceDirection(Vector3.forward, Vector3.back));
    }

    [Test]
    public void Angled_hit_reflects_off_the_wall()
    {
        Vector3 travel = new Vector3(1f, 0f, 1f).normalized;
        AssertClose(new Vector3(1f, 0f, -1f).normalized, LaunchArc.BounceDirection(travel, Vector3.back));
    }

    [Test]
    public void Bounce_stays_horizontal_on_a_sloped_surface()
    {
        Vector3 bounce = LaunchArc.BounceDirection(Vector3.forward, new Vector3(0f, 0.6f, -0.8f));
        Assert.That(bounce.y, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(bounce.magnitude, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void Bounce_without_a_usable_normal_reverses_travel()
    {
        AssertClose(Vector3.left, LaunchArc.BounceDirection(Vector3.right, Vector3.up));
    }
}
