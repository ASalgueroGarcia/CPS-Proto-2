using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EditMode unit tests for the Caterpillar's GDD state loop, settings asset,
/// head-only split vulnerability, split death persistence, and curved segment trail.
/// </summary>
public class CaterpillarTests
{
    private const string SettingsAssetPath = "Assets/_Game/Data/Enemies/CaterpillarSettings.asset";
    private const float Tolerance = 0.0001f;

    private CaterpillarSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = ScriptableObject.CreateInstance<CaterpillarSettings>();
    }

    [TearDown]
    public void TearDown()
    {
        if (settings != null)
            Object.DestroyImmediate(settings);
    }

    [Test]
    public void Settings_asset_loads_with_exact_gdd_defaults()
    {
        CaterpillarSettings asset = AssetDatabase.LoadAssetAtPath<CaterpillarSettings>(SettingsAssetPath);
        Assert.That(asset, Is.Not.Null, $"Failed to load {SettingsAssetPath}");

        Assert.That(asset.maxHealth, Is.EqualTo(40f).Within(Tolerance));
        Assert.That(asset.moveSpeed, Is.EqualTo(2f).Within(Tolerance));
        Assert.That(asset.detectionRange, Is.EqualTo(5f).Within(Tolerance));
        Assert.That(asset.attackRange, Is.EqualTo(4f).Within(Tolerance));
        Assert.That(asset.windupDuration, Is.EqualTo(1.2f).Within(Tolerance));
        Assert.That(asset.fallSpeed, Is.EqualTo(5f).Within(Tolerance));
        Assert.That(asset.slamCenterOffset, Is.EqualTo(2f).Within(Tolerance));
        Assert.That(asset.slamImpactRadius, Is.EqualTo(3f).Within(Tolerance));
        Assert.That(asset.slamDamage, Is.EqualTo(25f).Within(Tolerance));
        Assert.That(asset.slamKnockbackForce, Is.EqualTo(6f).Within(Tolerance));
        Assert.That(asset.splitDuration, Is.EqualTo(2f).Within(Tolerance));
        Assert.That(asset.reformDuration, Is.EqualTo(0.5f).Within(Tolerance));
        Assert.That(asset.attackCooldown, Is.EqualTo(3f).Within(Tolerance));
        Assert.That(asset.segmentCount, Is.EqualTo(6));
        Assert.That(asset.segmentSpacing, Is.EqualTo(0.55f).Within(Tolerance));
        Assert.That(asset.segmentRadius, Is.EqualTo(0.4f).Within(Tolerance));
        Assert.That(asset.headRadius, Is.EqualTo(0.48f).Within(Tolerance));
        Assert.That(asset.scatterRadius, Is.EqualTo(2.5f).Within(Tolerance));
        Assert.That(asset.trailFollowSharpness, Is.EqualTo(12f).Within(Tolerance));
        Assert.That(asset.SlamDuration, Is.EqualTo(0.4f).Within(Tolerance));
    }

    [Test]
    public void Patrol_becomes_permanently_aware_within_five_metres_and_never_resets()
    {
        var machine = new CaterpillarStateMachine(settings);
        Vector3 origin = Vector3.zero;

        machine.Tick(origin, new Vector3(0f, 0f, 5.1f), 0.1f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Patrol));
        Assert.That(machine.IsAware, Is.False);
        Assert.That(machine.CanMove, Is.True);

        machine.Tick(origin, new Vector3(0f, 0f, 5f), 0.1f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Chase));
        Assert.That(machine.IsAware, Is.True);

        machine.Tick(origin, new Vector3(0f, 0f, 25f), 1f);
        Assert.That(machine.IsAware, Is.True);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Chase));
    }

    [Test]
    public void Patrol_transitions_directly_to_windup_when_detected_inside_attack_range()
    {
        var machine = new CaterpillarStateMachine(settings);

        machine.Tick(Vector3.zero, new Vector3(0f, 0f, 3.5f), 0.1f);

        Assert.That(machine.IsAware, Is.True);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Windup));
        Assert.That(machine.EyesAlert, Is.True);
        Assert.That(machine.CanMove, Is.False);
    }

    [Test]
    public void Chase_steps_at_move_speed_and_begins_windup_at_four_metres()
    {
        var machine = new CaterpillarStateMachine(settings);
        Vector3 position = Vector3.zero;
        Vector3 player = new Vector3(0f, 0f, 5f);

        machine.Tick(position, player, 0.1f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Chase));
        AssertClose(new Vector3(0f, 0f, 2f), machine.DesiredVelocity);

        position = machine.MoveStep(position, player, 0.5f);
        AssertClose(new Vector3(0f, 0f, 1f), position);

        machine.Tick(position, player, 0.1f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Windup));
        Assert.That(machine.CanMove, Is.False);
    }

    [Test]
    public void Windup_tracks_player_then_locks_slam_direction_and_two_metre_impact_center()
    {
        var machine = new CaterpillarStateMachine(settings);
        Vector3 origin = new Vector3(1f, 0f, 2f);

        machine.Tick(origin, origin + Vector3.forward * 3f, 0f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Windup));
        Assert.That(machine.EyesAlert, Is.True);

        Vector3 playerEast = origin + Vector3.right * 3f;
        machine.Tick(origin, playerEast, 0.6f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Windup));
        Assert.That(machine.WindupProgress, Is.EqualTo(0.5f).Within(Tolerance));
        AssertClose(Vector3.right, machine.TrackedDirection);

        machine.Tick(origin, playerEast, 0.6f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Slam));
        Assert.That(machine.WindupProgress, Is.EqualTo(1f).Within(Tolerance));
        AssertClose(Vector3.right, machine.LockedSlamDirection);
        AssertClose(new Vector3(3f, 0f, 2f), machine.SlamImpactCenter);

        Vector3 playerBehind = origin + Vector3.left * 6f;
        machine.Tick(origin, playerBehind, 0.2f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Slam));
        Assert.That(machine.ImpactTriggeredThisTick, Is.False);
        AssertClose(Vector3.right, machine.LockedSlamDirection);
        AssertClose(new Vector3(3f, 0f, 2f), machine.SlamImpactCenter);
    }

    [Test]
    public void Slam_fires_one_shot_impact_and_checks_three_metre_horizontal_radius()
    {
        var machine = new CaterpillarStateMachine(settings);
        Vector3 origin = Vector3.zero;
        Vector3 player = new Vector3(0f, 0f, 3.5f);
        int impactCount = 0;
        Vector3 reportedCenter = Vector3.zero;
        machine.OnSlamImpact += center =>
        {
            impactCount++;
            reportedCenter = center;
        };

        Assert.That(machine.IsPointInSlamImpact(Vector3.zero), Is.False);

        machine.Tick(origin, player, 0f);
        machine.Tick(origin, player, 1.2f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Slam));
        Assert.That(impactCount, Is.EqualTo(0));

        machine.Tick(origin, player, 0.4f);
        Assert.That(machine.ImpactTriggeredThisTick, Is.True);
        Assert.That(impactCount, Is.EqualTo(1));
        AssertClose(new Vector3(0f, 0f, 2f), reportedCenter);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Split));

        Assert.That(machine.IsPointInSlamImpact(new Vector3(0f, 0f, 2f)), Is.True);
        Assert.That(machine.IsPointInSlamImpact(new Vector3(3f, 2.5f, 2f)), Is.True);
        Assert.That(machine.IsPointInSlamImpact(new Vector3(0f, 0f, 5f)), Is.True);
        Assert.That(machine.IsPointInSlamImpact(new Vector3(0f, 0f, 5.05f)), Is.False);

        machine.Tick(origin, player, 0.1f);
        Assert.That(machine.ImpactTriggeredThisTick, Is.False);
        Assert.That(impactCount, Is.EqualTo(1));
    }

    [Test]
    public void Split_lasts_two_seconds_and_only_head_piece_accepts_damage()
    {
        var machine = new CaterpillarStateMachine(settings);
        AdvanceToSplit(machine, Vector3.zero, new Vector3(0f, 0f, 3f));

        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Split));
        Assert.That(machine.CanMove, Is.False);
        Assert.That(machine.CanAttack, Is.False);
        Assert.That(machine.PiecesScattered, Is.True);
        Assert.That(machine.IsHeadOnlyVulnerable, Is.True);

        Assert.That(machine.ApplyDamage(15f, isHeadPiece: false), Is.False);
        Assert.That(machine.CurrentHealth, Is.EqualTo(40f).Within(Tolerance));

        Assert.That(machine.ApplyDamage(15f, isHeadPiece: true), Is.True);
        Assert.That(machine.CurrentHealth, Is.EqualTo(25f).Within(Tolerance));

        machine.Tick(Vector3.zero, new Vector3(0f, 0f, 3f), 1.9f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Split));

        machine.Tick(Vector3.zero, new Vector3(0f, 0f, 3f), 0.1f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Reform));
    }

    [Test]
    public void Reform_lasts_half_a_second_then_resets_eyes_and_starts_three_second_cooldown()
    {
        var machine = new CaterpillarStateMachine(settings);
        Vector3 origin = Vector3.zero;
        Vector3 player = new Vector3(0f, 0f, 3f);
        AdvanceToSplit(machine, origin, player);

        machine.Tick(origin, player, 2f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Reform));
        Assert.That(machine.CanMove, Is.False);
        Assert.That(machine.CanAttack, Is.False);
        Assert.That(machine.PiecesScattered, Is.True);
        Assert.That(machine.EyesAlert, Is.True);

        machine.Tick(origin, player, 0.25f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Reform));
        Assert.That(machine.ReformProgress, Is.EqualTo(0.5f).Within(Tolerance));

        machine.Tick(origin, player, 0.25f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Hold));
        Assert.That(machine.EyesAlert, Is.False);
        Assert.That(machine.PiecesScattered, Is.False);
        Assert.That(machine.CooldownRemaining, Is.EqualTo(3f).Within(Tolerance));
    }

    [Test]
    public void Hold_tracks_player_during_cooldown_chases_if_player_leaves_and_winds_up_when_ready()
    {
        var machine = new CaterpillarStateMachine(settings);
        Vector3 origin = Vector3.zero;
        Vector3 closePlayer = new Vector3(0f, 0f, 3f);
        AdvanceToHold(machine, origin, closePlayer);

        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Hold));
        Assert.That(machine.CanMove, Is.False);
        Assert.That(machine.CanAttack, Is.False);

        Vector3 sidePlayer = new Vector3(3f, 0f, 0f);
        machine.Tick(origin, sidePlayer, 1f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Hold));
        AssertClose(Vector3.right, machine.TrackedDirection);
        Assert.That(machine.CooldownRemaining, Is.EqualTo(2f).Within(Tolerance));

        Vector3 farPlayer = new Vector3(5f, 0f, 0f);
        machine.Tick(origin, farPlayer, 1f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Chase));
        Assert.That(machine.CanMove, Is.True);
        Assert.That(machine.CooldownRemaining, Is.EqualTo(1f).Within(Tolerance));

        machine.Tick(origin, sidePlayer, 0.5f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Hold));
        Assert.That(machine.CooldownRemaining, Is.EqualTo(0.5f).Within(Tolerance));

        machine.Tick(origin, sidePlayer, 0.5f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Windup));
        Assert.That(machine.EyesAlert, Is.True);
    }

    [Test]
    public void Death_while_intact_clears_alert_and_leaves_pieces_unscattered()
    {
        var machine = new CaterpillarStateMachine(settings);
        machine.Tick(Vector3.zero, new Vector3(0f, 0f, 3f), 0f);
        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Windup));

        machine.ApplyDamage(40f, isHeadPiece: false);

        Assert.That(machine.Phase, Is.EqualTo(CaterpillarPhase.Dead));
        Assert.That(machine.DiedWhileSplit, Is.False);
        Assert.That(machine.PiecesScattered, Is.False);
        Assert.That(machine.EyesAlert, Is.False);
        Assert.That(machine.CanMove, Is.False);
        Assert.That(machine.CanAttack, Is.False);
    }

    [Test]
    public void Death_during_split_or_reform_keeps_pieces_scattered_permanently()
    {
        var splitDeath = new CaterpillarStateMachine(settings);
        AdvanceToSplit(splitDeath, Vector3.zero, new Vector3(0f, 0f, 3f));

        splitDeath.ApplyDamage(40f, isHeadPiece: true);
        Assert.That(splitDeath.Phase, Is.EqualTo(CaterpillarPhase.Dead));
        Assert.That(splitDeath.DiedWhileSplit, Is.True);
        Assert.That(splitDeath.PiecesScattered, Is.True);

        splitDeath.Tick(Vector3.zero, new Vector3(0f, 0f, 3f), 5f);
        Assert.That(splitDeath.Phase, Is.EqualTo(CaterpillarPhase.Dead));
        Assert.That(splitDeath.PiecesScattered, Is.True);

        var reformDeath = new CaterpillarStateMachine(settings);
        AdvanceToSplit(reformDeath, Vector3.zero, new Vector3(0f, 0f, 3f));
        reformDeath.Tick(Vector3.zero, new Vector3(0f, 0f, 3f), 2f);
        Assert.That(reformDeath.Phase, Is.EqualTo(CaterpillarPhase.Reform));

        reformDeath.OnDeath();
        Assert.That(reformDeath.Phase, Is.EqualTo(CaterpillarPhase.Dead));
        Assert.That(reformDeath.DiedWhileSplit, Is.True);
        Assert.That(reformDeath.PiecesScattered, Is.True);
    }

    [Test]
    public void Trail_around_right_angle_turn_places_non_collinear_segments_at_exact_spacing()
    {
        var trail = new CaterpillarTrailMath(settings.segmentCount, settings.segmentSpacing);
        trail.Reset(Vector3.zero, Vector3.right);

        for (int i = 1; i <= 40; i++)
            trail.RecordHeadPosition(new Vector3(i * 0.05f, 0f, 0f));

        for (int i = 1; i <= 28; i++)
            trail.RecordHeadPosition(new Vector3(2f, 0f, i * 0.05f));

        IReadOnlyList<Vector3> positions = trail.TargetPositions;
        AssertClose(new Vector3(2f, 0f, 1.4f), positions[0]);

        for (int i = 1; i < positions.Count; i++)
        {
            float distance = Vector3.Distance(positions[i - 1], positions[i]);
            Assert.That(distance, Is.EqualTo(settings.segmentSpacing).Within(Tolerance), $"segment {i}");
        }

        Assert.That(positions[1].x, Is.EqualTo(2f).Within(Tolerance));
        Assert.That(positions[positions.Count - 1].z, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(MaxDistanceFromChord(positions), Is.GreaterThan(0.25f));
    }

    [Test]
    public void Smoothed_trail_along_curved_arc_preserves_spacing_and_curvature()
    {
        var trail = new CaterpillarTrailMath(settings.segmentCount, settings.segmentSpacing);
        float radius = 2f;
        trail.Reset(new Vector3(radius, 0f, 0f), Vector3.forward);

        const int steps = 60;
        for (int i = 1; i <= steps; i++)
        {
            float angle = (i / (float)steps) * Mathf.PI * 0.75f;
            Vector3 head = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            trail.UpdateSmoothed(head, settings.trailFollowSharpness, 1f / 60f);
        }

        IReadOnlyList<Vector3> smoothed = trail.SmoothedPositions;
        for (int i = 1; i < smoothed.Count; i++)
        {
            float distance = Vector3.Distance(smoothed[i - 1], smoothed[i]);
            Assert.That(distance, Is.EqualTo(settings.segmentSpacing).Within(Tolerance), $"smoothed {i}");
        }

        Assert.That(MaxDistanceFromChord(smoothed), Is.GreaterThan(0.15f));
    }

    private static void AdvanceToSplit(CaterpillarStateMachine machine, Vector3 origin, Vector3 player)
    {
        machine.Tick(origin, player, 0f);
        machine.Tick(origin, player, 1.2f);
        machine.Tick(origin, player, 0.4f);
    }

    private static void AdvanceToHold(CaterpillarStateMachine machine, Vector3 origin, Vector3 player)
    {
        AdvanceToSplit(machine, origin, player);
        machine.Tick(origin, player, 2f);
        machine.Tick(origin, player, 0.5f);
    }

    private static float MaxDistanceFromChord(IReadOnlyList<Vector3> points)
    {
        Vector3 start = points[0];
        Vector3 chord = points[points.Count - 1] - start;
        float length = chord.magnitude;
        if (length <= Tolerance)
            return 0f;

        Vector3 direction = chord / length;
        float maxDeviation = 0f;
        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 relative = points[i] - start;
            Vector3 projected = direction * Vector3.Dot(relative, direction);
            float deviation = (relative - projected).magnitude;
            if (deviation > maxDeviation)
                maxDeviation = deviation;
        }

        return maxDeviation;
    }

    private static void AssertClose(Vector3 expected, Vector3 actual) =>
        Assert.That(Vector3.Distance(expected, actual), Is.LessThan(Tolerance), $"expected {expected}, got {actual}");
}
