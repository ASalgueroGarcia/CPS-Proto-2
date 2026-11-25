using UnityEngine;

/// <summary>
/// Pure, stateless math and rule helpers for pit dash-to-fall detection, drop trajectories,
/// safe respawn clamping, and semi-transparent respawn blink curves.
/// </summary>
public static class PitFallRules
{
    public const float DefaultDamage = 15f;
    public const float DefaultFallDuration = 0.35f;
    public const float DefaultFallDepth = 2.2f;
    public const float DefaultRespawnClearance = 0.9f;
    public const float DefaultBlinkDuration = 1.2f;
    public const float DefaultBlinkInterval = 0.12f;
    public const float DefaultMinAlpha = 0.35f;
    public const float DefaultInvulnerabilityDuration = 1.2f;
    public const float MinInwardDashDot = 0.15f;

    public static bool ShouldTriggerFall(
        bool isDashing,
        bool isAlreadyFalling,
        Vector3 dashDirection,
        Vector3 hitNormal,
        Vector3 playerPosition,
        Bounds pitBounds)
    {
        if (!isDashing || isAlreadyFalling)
            return false;

        Vector3 flatDash = new Vector3(dashDirection.x, 0f, dashDirection.z);
        if (flatDash.sqrMagnitude < 0.0001f)
            return false;

        flatDash.Normalize();

        if (IsInsidePitXZ(playerPosition, pitBounds))
            return true;

        Vector3 flatNormal = new Vector3(hitNormal.x, 0f, hitNormal.z);
        if (flatNormal.sqrMagnitude > 0.0001f)
        {
            float inwardDot = Vector3.Dot(flatDash, -flatNormal.normalized);
            return inwardDot >= MinInwardDashDot;
        }

        Vector3 closest = new Vector3(
            Mathf.Clamp(playerPosition.x, pitBounds.min.x, pitBounds.max.x),
            0f,
            Mathf.Clamp(playerPosition.z, pitBounds.min.z, pitBounds.max.z));
        Vector3 toPit = closest - new Vector3(playerPosition.x, 0f, playerPosition.z);

        if (toPit.sqrMagnitude < 0.0001f)
        {
            toPit = new Vector3(
                pitBounds.center.x - playerPosition.x,
                0f,
                pitBounds.center.z - playerPosition.z);
        }

        return toPit.sqrMagnitude < 0.0001f || Vector3.Dot(flatDash, toPit.normalized) >= MinInwardDashDot;
    }

    public static bool IsInsidePitXZ(Vector3 position, Bounds pitBounds, float margin = 0f)
    {
        return position.x > pitBounds.min.x - margin
            && position.x < pitBounds.max.x + margin
            && position.z > pitBounds.min.z - margin
            && position.z < pitBounds.max.z + margin;
    }

    public static Vector3 ComputeSafeRespawnPosition(Vector3 lastSafePosition, Bounds pitBounds, float clearance)
    {
        float safeClearance = Mathf.Max(0f, clearance);
        float minX = pitBounds.min.x - safeClearance;
        float maxX = pitBounds.max.x + safeClearance;
        float minZ = pitBounds.min.z - safeClearance;
        float maxZ = pitBounds.max.z + safeClearance;

        if (lastSafePosition.x <= minX || lastSafePosition.x >= maxX
            || lastSafePosition.z <= minZ || lastSafePosition.z >= maxZ)
        {
            return lastSafePosition;
        }

        float dxLeft = lastSafePosition.x - minX;
        float dxRight = maxX - lastSafePosition.x;
        float dzBack = lastSafePosition.z - minZ;
        float dzFront = maxZ - lastSafePosition.z;

        float minPush = dxLeft;
        Vector3 resolved = new Vector3(minX, lastSafePosition.y, lastSafePosition.z);

        if (dxRight < minPush)
        {
            minPush = dxRight;
            resolved = new Vector3(maxX, lastSafePosition.y, lastSafePosition.z);
        }

        if (dzBack < minPush)
        {
            minPush = dzBack;
            resolved = new Vector3(lastSafePosition.x, lastSafePosition.y, minZ);
        }

        if (dzFront < minPush)
        {
            resolved = new Vector3(lastSafePosition.x, lastSafePosition.y, maxZ);
        }

        return resolved;
    }

    public static Vector3 ComputeFallDropTarget(
        Vector3 startPosition,
        Vector3 dashDirection,
        Bounds pitBounds,
        float fallDepth,
        float entryInset = 0.65f)
    {
        Vector3 flatDash = new Vector3(dashDirection.x, 0f, dashDirection.z);
        Vector3 stepXZ = flatDash.sqrMagnitude > 0.0001f
            ? flatDash.normalized * (Mathf.Max(0f, entryInset) + 0.5f)
            : Vector3.zero;

        Vector3 candidate = startPosition + stepXZ;
        float innerPadX = Mathf.Min(0.35f, pitBounds.extents.x * 0.5f);
        float innerPadZ = Mathf.Min(0.35f, pitBounds.extents.z * 0.5f);

        float targetX = Mathf.Clamp(candidate.x, pitBounds.min.x + innerPadX, pitBounds.max.x - innerPadX);
        float targetZ = Mathf.Clamp(candidate.z, pitBounds.min.z + innerPadZ, pitBounds.max.z - innerPadZ);
        float targetY = startPosition.y - Mathf.Max(0f, fallDepth);

        return new Vector3(targetX, targetY, targetZ);
    }

    public static Vector3 EvaluateFallPosition(Vector3 startPosition, Vector3 dropTarget, float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        float horizontalT = 1f - (1f - t) * (1f - t);
        float verticalT = t * t;

        return new Vector3(
            Mathf.Lerp(startPosition.x, dropTarget.x, horizontalT),
            Mathf.Lerp(startPosition.y, dropTarget.y, verticalT),
            Mathf.Lerp(startPosition.z, dropTarget.z, horizontalT));
    }

    public static float EvaluateBlinkAlpha(float elapsed, float duration, float interval, float minAlpha)
    {
        if (duration <= 0f || elapsed < 0f || elapsed >= duration)
            return 1f;

        float clampedMin = Mathf.Clamp01(minAlpha);
        if (interval <= 0.0001f)
            return clampedMin;

        float wave = 0.5f * (1f - Mathf.Cos(Mathf.PI * elapsed / interval));
        return Mathf.Lerp(clampedMin, 1f, wave);
    }
}
