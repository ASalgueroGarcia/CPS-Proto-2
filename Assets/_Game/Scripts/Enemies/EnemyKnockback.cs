using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Owns an enemy's knockback. A hit pushes the enemy away from its source; if the
/// push runs into a wall or obstacle the enemy is launched back off it in an arc.
/// Running into an explosive sets it off first, so the enemy takes the blast as it flies.
/// Added by <see cref="Enemy"/> when the prefab has launch settings assigned.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyKnockback : MonoBehaviour
{
    private Enemy enemy;
    private EnemyLaunchSettings settings;
    private Coroutine routine;

    /// <summary>True while the enemy is in the air. Its AI and NavMeshAgent are off.</summary>
    public bool IsLaunched { get; private set; }

    public void Initialize(Enemy owner, EnemyLaunchSettings launchSettings)
    {
        enemy = owner;
        settings = launchSettings;
        enemy.Health.useInternalKnockback = false;
        enemy.Health.OnKnockbackReceived.AddListener(Knockback);
    }

    /// <summary>Pushes the enemy away from source. Ignored while it is in the air.</summary>
    public void Knockback(Vector3 source, float force)
    {
        if (IsLaunched || settings == null) return;

        Vector3 direction = transform.position - source;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PushRoutine(direction.normalized, force));
    }

    /// <summary>Throws the enemy in an arc along direction. Ignored when it is off the NavMesh.</summary>
    public void Launch(Vector3 direction, float force)
    {
        if (IsLaunched || settings == null || !CanLaunch()) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(LaunchRoutine(direction, force));
    }

    private IEnumerator PushRoutine(Vector3 direction, float force)
    {
        float elapsed = 0f;
        while (elapsed < settings.pushDuration)
        {
            float step = force * settings.pushSpeedPerForce * (1f - elapsed / settings.pushDuration) * Time.deltaTime;

            if (force >= settings.minLaunchForce && CanLaunch() && HitsSomething(direction, step, out Vector3 normal))
            {
                yield return LaunchRoutine(LaunchArc.BounceDirection(direction, normal), force);
                yield break;
            }

            // Agent.Move keeps the enemy on the NavMesh, so a push never crosses a pit edge.
            if (CanLaunch()) enemy.Agent.Move(direction * step);
            else transform.position += direction * step;

            elapsed += Time.deltaTime;
            yield return null;
        }

        routine = null;
    }

    private IEnumerator LaunchRoutine(Vector3 direction, float force)
    {
        IsLaunched = true;
        enemy.ChangeState(Enemy.EnemyState.Idle);
        enemy.enabled = false;

        Vector3 start = transform.position;
        Vector3 end = LandingPoint(start, direction, settings.LaunchDistance(force));
        enemy.Agent.enabled = false;

        float elapsed = 0f;
        while (elapsed < settings.launchDuration)
        {
            transform.position = LaunchArc.Evaluate(start, end, settings.launchHeight, elapsed / settings.launchDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        enemy.Agent.enabled = true;
        enemy.Agent.Warp(end);
        enemy.enabled = true;
        IsLaunched = false;
        routine = null;
    }

    private bool CanLaunch() => enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh;

    private bool HitsSomething(Vector3 direction, float step, out Vector3 normal)
    {
        IExplosive explosive = ExplosiveAhead(direction, step, out normal);
        if (explosive == null) return HitsWall(direction, step, out normal);

        explosive.Detonate();
        return true;
    }

    // Explosives have no solid collider, only a wide trigger, so contact is measured against their visible body.
    private IExplosive ExplosiveAhead(Vector3 direction, float step, out Vector3 normal)
    {
        Vector3 origin = transform.position + Vector3.up * settings.probeHeight;
        float reach = settings.probeRadius + settings.probeDistance + step;

        foreach (Collider near in Physics.OverlapSphere(origin, reach, settings.wallLayers, QueryTriggerInteraction.Collide))
        {
            IExplosive explosive = near.GetComponentInParent<IExplosive>();
            if (explosive == null) continue;

            Renderer body = ((Component)explosive).GetComponentInChildren<Renderer>();
            Vector3 toBody = (body != null ? body.bounds.ClosestPoint(origin) : near.transform.position) - origin;
            toBody.y = 0f;
            if (toBody.magnitude > reach || Vector3.Dot(toBody, direction) <= 0f) continue;

            normal = -toBody.normalized;
            return explosive;
        }

        normal = default;
        return null;
    }

    private bool HitsWall(Vector3 direction, float step, out Vector3 normal)
    {
        Vector3 origin = transform.position + Vector3.up * settings.probeHeight;
        RaycastHit[] hits = Physics.SphereCastAll(origin, settings.probeRadius, direction, step + settings.probeDistance,
            settings.wallLayers, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            // distance 0 means the probe started inside the collider and has no usable normal.
            if (hit.distance <= 0f || hit.collider.transform.IsChildOf(transform)) continue;

            normal = hit.normal;
            return true;
        }

        normal = default;
        return false;
    }

    // The NavMesh raycast stops at the first edge, so the landing never crosses a pit or an obstacle.
    private static Vector3 LandingPoint(Vector3 start, Vector3 direction, float distance)
    {
        if (!NavMesh.SamplePosition(start, out NavMeshHit from, 1f, NavMesh.AllAreas))
            return start;

        Vector3 target = from.position + direction * distance;
        return NavMesh.Raycast(from.position, target, out NavMeshHit edge, NavMesh.AllAreas) ? edge.position : target;
    }

    private void OnDestroy()
    {
        if (enemy != null && enemy.Health != null)
            enemy.Health.OnKnockbackReceived.RemoveListener(Knockback);
    }
}
