using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Path-history trail solver for the Caterpillar's body segments. Segments follow the
/// exact curve walked by the head at constant spacing so turns bend smoothly instead of
/// swinging the body as a rigid rod.
/// </summary>
public class CaterpillarTrailMath
{
    private readonly List<Vector3> waypoints = new List<Vector3>();
    private readonly Vector3[] targetPositions;
    private readonly Vector3[] smoothedPositions;
    private readonly int segmentCount;
    private readonly float segmentSpacing;
    private readonly float minSampleDistance;
    private Vector3 initialForward = Vector3.forward;

    public int SegmentCount => segmentCount;
    public float SegmentSpacing => segmentSpacing;
    public IReadOnlyList<Vector3> TargetPositions => targetPositions;
    public IReadOnlyList<Vector3> SmoothedPositions => smoothedPositions;

    public CaterpillarTrailMath(int segmentCount, float segmentSpacing, float minSampleDistance = 0.02f)
    {
        if (segmentCount < 2)
            throw new ArgumentOutOfRangeException(nameof(segmentCount), "Caterpillar requires at least 2 segments.");
        if (segmentSpacing <= 0f)
            throw new ArgumentOutOfRangeException(nameof(segmentSpacing), "Segment spacing must be positive.");

        this.segmentCount = segmentCount;
        this.segmentSpacing = segmentSpacing;
        this.minSampleDistance = Mathf.Max(0.001f, minSampleDistance);
        targetPositions = new Vector3[segmentCount];
        smoothedPositions = new Vector3[segmentCount];

        Reset(Vector3.zero, Vector3.forward);
    }

    /// <summary>Seeds a straight initial trail behind headPosition opposite forward.</summary>
    public void Reset(Vector3 headPosition, Vector3 forward)
    {
        initialForward = NormalizeHorizontal(forward, Vector3.forward);
        float chainLength = (segmentCount - 1) * segmentSpacing;

        waypoints.Clear();
        waypoints.Add(headPosition - initialForward * (chainLength + segmentSpacing));
        waypoints.Add(headPosition);
        waypoints.Add(headPosition);

        EvaluateFromPath(waypoints, segmentCount, segmentSpacing, initialForward, targetPositions);
        Array.Copy(targetPositions, smoothedPositions, segmentCount);
    }

    /// <summary>Records a new head position and updates exact path-history TargetPositions.</summary>
    public void RecordHeadPosition(Vector3 headPosition)
    {
        if (waypoints.Count < 2)
        {
            Reset(headPosition, initialForward);
            return;
        }

        waypoints[waypoints.Count - 1] = headPosition;

        Vector3 lastCommitted = waypoints[waypoints.Count - 2];
        if ((headPosition - lastCommitted).sqrMagnitude >= minSampleDistance * minSampleDistance)
        {
            waypoints.Add(headPosition);
            TrimExcessHistory();
        }

        EvaluateFromPath(waypoints, segmentCount, segmentSpacing, initialForward, targetPositions);
    }

    /// <summary>
    /// Records headPosition and exponentially smooths SmoothedPositions toward the curved
    /// path targets while preserving exact segmentSpacing between adjacent pieces.
    /// </summary>
    public void UpdateSmoothed(Vector3 headPosition, float followSharpness, float deltaTime)
    {
        RecordHeadPosition(headPosition);

        if (followSharpness <= 0f || deltaTime <= 0f)
        {
            Array.Copy(targetPositions, smoothedPositions, segmentCount);
            return;
        }

        float blend = 1f - Mathf.Exp(-followSharpness * deltaTime);
        smoothedPositions[0] = headPosition;

        for (int i = 1; i < segmentCount; i++)
        {
            Vector3 desired = Vector3.Lerp(smoothedPositions[i], targetPositions[i], blend);
            Vector3 offset = desired - smoothedPositions[i - 1];
            Vector3 direction = offset.sqrMagnitude > 1e-8f
                ? offset.normalized
                : (targetPositions[i] - smoothedPositions[i - 1]).normalized;

            if (direction.sqrMagnitude <= 1e-8f)
                direction = -initialForward;

            smoothedPositions[i] = smoothedPositions[i - 1] + direction * segmentSpacing;
        }
    }

    /// <summary>
    /// Samples segmentCount positions along chronologicalPath (oldest first, current head last)
    /// so every segment lies on the recorded curve at exact Euclidean distance segmentSpacing
    /// from the segment ahead of it.
    /// </summary>
    public static Vector3[] EvaluateFromPath(
        IReadOnlyList<Vector3> chronologicalPath,
        int segmentCount,
        float segmentSpacing,
        Vector3 fallbackForward = default)
    {
        if (segmentCount < 2)
            throw new ArgumentOutOfRangeException(nameof(segmentCount));
        if (segmentSpacing <= 0f)
            throw new ArgumentOutOfRangeException(nameof(segmentSpacing));

        Vector3[] results = new Vector3[segmentCount];
        EvaluateFromPath(chronologicalPath, segmentCount, segmentSpacing, fallbackForward, results);
        return results;
    }

    public static void EvaluateFromPath(
        IReadOnlyList<Vector3> chronologicalPath,
        int segmentCount,
        float segmentSpacing,
        Vector3 fallbackForward,
        Vector3[] results)
    {
        if (chronologicalPath == null || chronologicalPath.Count == 0)
            throw new ArgumentException("Path must contain at least one point.", nameof(chronologicalPath));
        if (results == null || results.Length < segmentCount)
            throw new ArgumentException("Results buffer is smaller than segmentCount.", nameof(results));
        if (segmentCount < 2)
            throw new ArgumentOutOfRangeException(nameof(segmentCount));
        if (segmentSpacing <= 0f)
            throw new ArgumentOutOfRangeException(nameof(segmentSpacing));

        int headIndex = chronologicalPath.Count - 1;
        Vector3 anchor = chronologicalPath[headIndex];
        results[0] = anchor;

        int upperIndex = headIndex;
        Vector3 segmentStart = anchor;
        float spacingSqr = segmentSpacing * segmentSpacing;
        Vector3 tailDirection = ResolveTailDirection(chronologicalPath, fallbackForward);

        for (int seg = 1; seg < segmentCount; seg++)
        {
            bool placed = false;

            while (upperIndex > 0)
            {
                Vector3 olderPoint = chronologicalPath[upperIndex - 1];
                if ((olderPoint - anchor).sqrMagnitude >= spacingSqr)
                {
                    Vector3 hit = IntersectSphereSegment(anchor, segmentSpacing, segmentStart, olderPoint);
                    results[seg] = hit;
                    anchor = hit;
                    segmentStart = hit;
                    placed = true;
                    break;
                }

                upperIndex--;
                segmentStart = olderPoint;
            }

            if (!placed)
            {
                Vector3 extrapolated = anchor + tailDirection * segmentSpacing;
                results[seg] = extrapolated;
                anchor = extrapolated;
                segmentStart = extrapolated;
            }
        }
    }

    /// <summary>Deterministic horizontal scatter offset for piece index during Split.</summary>
    public static Vector3 ComputeScatterOffset(int segmentIndex, int segmentCount, float scatterRadius, Vector3 forward)
    {
        if (scatterRadius <= 0f || segmentCount <= 0)
            return Vector3.zero;

        Vector3 basisForward = NormalizeHorizontal(forward, Vector3.forward);
        if (segmentIndex <= 0)
            return basisForward * (scatterRadius * 0.35f);

        int bodyCount = Mathf.Max(1, segmentCount - 1);
        float angleDegrees = ((segmentIndex - 1f) / bodyCount) * 360f + 30f;
        Quaternion rotation = Quaternion.AngleAxis(angleDegrees, Vector3.up);
        return rotation * basisForward * scatterRadius;
    }

    /// <summary>Smoothly interpolates a scattered piece back to its chain slot over Reform (0..1).</summary>
    public static Vector3 EvaluateReformPosition(Vector3 scatteredPosition, Vector3 targetChainPosition, float reformProgress)
    {
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(reformProgress));
        return Vector3.LerpUnclamped(scatteredPosition, targetChainPosition, t);
    }

    private void TrimExcessHistory()
    {
        float requiredLength = (segmentCount + 1) * segmentSpacing;
        float accumulated = 0f;

        for (int i = waypoints.Count - 1; i > 0; i--)
        {
            accumulated += Vector3.Distance(waypoints[i], waypoints[i - 1]);
            if (accumulated > requiredLength && i - 1 > 0)
            {
                waypoints.RemoveRange(0, i - 1);
                return;
            }
        }
    }

    private static Vector3 IntersectSphereSegment(Vector3 center, float radius, Vector3 insidePoint, Vector3 outsidePoint)
    {
        Vector3 d = outsidePoint - insidePoint;
        float a = Vector3.Dot(d, d);
        if (a < 1e-10f)
            return outsidePoint;

        Vector3 f = insidePoint - center;
        float b = Vector3.Dot(f, d);
        float c = Vector3.Dot(f, f) - radius * radius;
        float discriminant = Mathf.Max(0f, b * b - a * c);
        float t = Mathf.Clamp01((-b + Mathf.Sqrt(discriminant)) / a);
        return insidePoint + d * t;
    }

    private static Vector3 ResolveTailDirection(IReadOnlyList<Vector3> chronologicalPath, Vector3 fallbackForward)
    {
        for (int i = 0; i < chronologicalPath.Count - 1; i++)
        {
            Vector3 backward = chronologicalPath[i] - chronologicalPath[i + 1];
            if (backward.sqrMagnitude > 1e-8f)
                return backward.normalized;
        }

        return -NormalizeHorizontal(fallbackForward, Vector3.forward);
    }

    private static Vector3 NormalizeHorizontal(Vector3 vector, Vector3 fallback)
    {
        vector.y = 0f;
        if (vector.sqrMagnitude > 0.0001f)
            return vector.normalized;

        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
    }
}
