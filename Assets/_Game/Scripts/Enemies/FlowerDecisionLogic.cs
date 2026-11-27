using UnityEngine;

public static class FlowerDecisionLogic
{
    public static FlowerAction EvaluateAction(
        int tier,
        float distanceToPlayer,
        bool isAware,
        bool tookDamage,
        bool biteCooldownDone,
        bool shotCooldownDone,
        bool burrowCooldownDone,
        bool isGrown,
        bool lastShotWasNormal,
        int ownFlowersAlive,
        FlowerSettings settings)
    {
        if (settings == null) return FlowerAction.Idle;

        if (distanceToPlayer <= settings.biteRange)
        {
            return biteCooldownDone ? FlowerAction.Bite : FlowerAction.Idle;
        }

        if (tier >= settings.burrowMinTier)
        {
            bool awareNow = isAware || distanceToPlayer <= settings.detectionRange || tookDamage;
            if (awareNow && burrowCooldownDone)
            {
                return FlowerAction.Burrow;
            }
        }

        if (distanceToPlayer <= settings.projectileRange)
        {
            if (!shotCooldownDone) return FlowerAction.Idle;

            bool canPollinate = tier >= settings.pollinatingMinTier
                && isGrown
                && lastShotWasNormal
                && ownFlowersAlive < settings.maxSpawnedFlowersAlive;

            return canPollinate ? FlowerAction.PollinatingShot : FlowerAction.NormalShot;
        }

        return FlowerAction.Idle;
    }

    public static Vector3 ClampBurrowTarget(Vector3 origin, Vector3 playerPosition, float maxHopDistance)
    {
        Vector3 flatOrigin = new Vector3(origin.x, origin.y, origin.z);
        Vector3 delta = new Vector3(playerPosition.x - origin.x, 0f, playerPosition.z - origin.z);
        float maxHop = Mathf.Max(0f, maxHopDistance);
        if (delta.magnitude > maxHop)
        {
            delta = delta.normalized * maxHop;
        }

        return flatOrigin + delta;
    }

    public static Vector3 EvaluateCurvedSeedPosition(Vector3 start, Vector3 end, float arcHeight, float t)
    {
        float clamped = Mathf.Clamp01(t);
        Vector3 pos = Vector3.Lerp(start, end, clamped);
        pos.y += 4f * arcHeight * clamped * (1f - clamped);
        return pos;
    }
}
