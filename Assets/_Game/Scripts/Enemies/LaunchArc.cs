using UnityEngine;

/// <summary>Math for an enemy launched off a wall. No scene access, so it is testable.</summary>
public static class LaunchArc
{
    /// <summary>Position on a parabola from start to end that peaks at height when t is 0.5.</summary>
    public static Vector3 Evaluate(Vector3 start, Vector3 end, float height, float t)
    {
        t = Mathf.Clamp01(t);
        Vector3 position = Vector3.Lerp(start, end, t);
        position.y += 4f * height * t * (1f - t);
        return position;
    }

    /// <summary>Horizontal direction an enemy leaves a wall in, given how it was travelling.</summary>
    public static Vector3 BounceDirection(Vector3 travel, Vector3 wallNormal)
    {
        travel.y = 0f;
        wallNormal.y = 0f;
        if (wallNormal.sqrMagnitude < 0.0001f)
            return -travel.normalized;

        return Vector3.Reflect(travel.normalized, wallNormal.normalized);
    }
}
