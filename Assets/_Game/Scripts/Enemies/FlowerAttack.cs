using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FlowerAttack : MonoBehaviour, IEnemyAttackStrategy, IEnemyAttackStateExitHandler, IEnemySpawner
{
    private static readonly Color BiteColor = new Color(1f, 0.5f, 0f);
    private static readonly Color PollinateColor = new Color(0.85f, 0.35f, 0.9f);

    public event Action<GameObject> OnEnemySpawned;

    [Header("Data")]
    [SerializeField] private FlowerSettings settings;
    [Tooltip("0 uses FlowerSettings.defaultTier; 1-3 overrides tier for testing or run progression.")]
    [Range(0, 3)] [SerializeField] private int tierOverride;
    [Tooltip("Optional prefab spawned by Pollinating shots. Defaults to cloning this Flower.")]
    [SerializeField] private GameObject spawnedFlowerPrefab;

    [Header("Visuals & References")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private Material projectileMaterial;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Material crackMaterial;

    private EnemyVisualFeedback visualFeedback;
    private Health health;
    private NavMeshAgent agent;
    private Collider bodyCollider;
    private Transform crack;
    private Vector3 baseVisualScale = Vector3.one;

    private readonly List<GameObject> ownSpawnedFlowers = new List<GameObject>();
    private FlowerAction currentAction = FlowerAction.Idle;
    private Vector3 burrowOrigin;
    private Vector3 burrowDestination;
    private float aliveTimer;
    private float biteCooldownTimer;
    private float shotCooldownTimer;
    private float burrowCooldownTimer;
    private float phaseTimer;
    private bool isAware;
    private bool tookDamage;
    private bool isGrown;
    private bool lastShotWasNormal;
    private bool burrowing;
    private bool actionFinished;
    private bool executeFired;

    public FlowerSettings Settings => settings;
    public int ActiveTier => tierOverride > 0 ? tierOverride : (settings != null ? settings.defaultTier : 2);
    public bool IsGrown => isGrown;
    public FlowerAction CurrentAction => currentAction;
    public int OwnAliveFlowerCount => CountAliveSpawnedFlowers();

    public float ApproachDuration => 0f;
    public float ExecutingDuration => 0.1f;
    public bool IsExecutionComplete => actionFinished;
    public bool BeginsWindupAtAnyDistance => true;

    public void SetTier(int tier)
    {
        tierOverride = Mathf.Clamp(tier, 0, 3);
    }

    private void Awake()
    {
        if (settings == null) settings = ScriptableObject.CreateInstance<FlowerSettings>();
        visualFeedback = GetComponent<EnemyVisualFeedback>();
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        bodyCollider = GetComponent<Collider>();
        if (visualRoot != null) baseVisualScale = visualRoot.localScale;
    }

    private void OnEnable()
    {
        if (health != null) health.OnDamageTaken.AddListener(HandleDamageTaken);
    }

    private void OnDisable()
    {
        if (health != null) health.OnDamageTaken.RemoveListener(HandleDamageTaken);
        ResetBurrow();
        if (visualFeedback != null) visualFeedback.ClearTelegraph();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        aliveTimer += dt;
        if (!isGrown && settings != null && aliveTimer >= settings.growDuration)
        {
            isGrown = true;
            if (visualRoot != null) visualRoot.localScale = baseVisualScale * settings.grownScaleMultiplier;
        }

        if (biteCooldownTimer > 0f) biteCooldownTimer = Mathf.Max(0f, biteCooldownTimer - dt);
        if (shotCooldownTimer > 0f) shotCooldownTimer = Mathf.Max(0f, shotCooldownTimer - dt);
        if (burrowCooldownTimer > 0f) burrowCooldownTimer = Mathf.Max(0f, burrowCooldownTimer - dt);
    }

    public void OnApproachTarget(Enemy owner, float distanceToPlayer)
    {
        if (owner == null || owner.PlayerTransform == null) return;
        if (owner.Agent != null && owner.Agent.enabled) owner.Agent.ResetPath();
        Face(owner, owner.PlayerTransform.position);
        UpdateAwareness(distanceToPlayer);
    }

    public void OnWindup(Enemy owner)
    {
        if (owner == null || owner.PlayerTransform == null) return;
        if (owner.Agent != null && owner.Agent.enabled) owner.Agent.ResetPath();

        float distance = FlatDistance(owner.transform.position, owner.PlayerTransform.position);
        UpdateAwareness(distance);

        if (currentAction == FlowerAction.Idle)
        {
            currentAction = FlowerDecisionLogic.EvaluateAction(
                ActiveTier,
                distance,
                isAware,
                tookDamage,
                biteCooldownTimer <= 0f,
                shotCooldownTimer <= 0f,
                burrowCooldownTimer <= 0f,
                isGrown,
                lastShotWasNormal,
                CountAliveSpawnedFlowers(),
                settings);
            phaseTimer = 0f;
            executeFired = false;
            actionFinished = currentAction == FlowerAction.Idle;
        }

        if (currentAction == FlowerAction.Idle)
        {
            if (visualFeedback != null) visualFeedback.ClearTelegraph();
            return;
        }

        phaseTimer += Time.deltaTime;
        Face(owner, owner.PlayerTransform.position);

        if (currentAction == FlowerAction.Bite)
        {
            float t = settings.biteWindup > 0f ? Mathf.Clamp01(phaseTimer / settings.biteWindup) : 1f;
            if (visualFeedback != null) visualFeedback.SetTelegraph(Color.Lerp(visualFeedback.BaseColor, BiteColor, t));
        }
        else if (currentAction == FlowerAction.NormalShot || currentAction == FlowerAction.PollinatingShot)
        {
            Color targetColor = currentAction == FlowerAction.PollinatingShot ? PollinateColor : Color.red;
            float t = Mathf.Clamp01(phaseTimer / 0.25f);
            if (visualFeedback != null) visualFeedback.SetTelegraph(Color.Lerp(visualFeedback.BaseColor, targetColor, t));
        }
        else if (currentAction == FlowerAction.Burrow)
        {
            float windup = Mathf.Max(0.05f, settings.burrowWindup);
            float t = Mathf.Clamp01(phaseTimer / windup);
            SetSink(t);
        }
    }

    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        if (owner == null || owner.PlayerTransform == null)
        {
            actionFinished = true;
            return;
        }

        switch (currentAction)
        {
            case FlowerAction.Idle:
                actionFinished = true;
                break;

            case FlowerAction.Bite:
                if (!executeFired)
                {
                    executeFired = true;
                    float dist = FlatDistance(owner.transform.position, owner.PlayerTransform.position);
                    if (dist <= settings.biteRange)
                    {
                        owner.PlayerHealth?.TakeDamage(settings.biteDamage, owner.transform.position);
                    }
                    biteCooldownTimer = settings.biteCooldown;
                    actionFinished = true;
                }
                break;

            case FlowerAction.NormalShot:
                if (!executeFired)
                {
                    executeFired = true;
                    ShootNormal(owner);
                    lastShotWasNormal = true;
                    shotCooldownTimer = settings.projectileCooldown;
                    actionFinished = true;
                }
                break;

            case FlowerAction.PollinatingShot:
                if (!executeFired)
                {
                    executeFired = true;
                    ShootPollinating(owner);
                    lastShotWasNormal = false;
                    shotCooldownTimer = settings.projectileCooldown;
                    actionFinished = true;
                }
                break;

            case FlowerAction.Burrow:
                TickBurrow(owner);
                break;
        }
    }

    public void OnCooldown(Enemy owner, float distanceToPlayer)
    {
        if (visualFeedback != null) visualFeedback.ClearTelegraph();
    }

    public void OnAttackStateExit(Enemy owner, EnemyAttackStateMachine.State state)
    {
        if (state == EnemyAttackStateMachine.State.Executing)
        {
            if (burrowing) ResetBurrow();
            currentAction = FlowerAction.Idle;
            phaseTimer = 0f;
            executeFired = false;
            actionFinished = false;
        }
    }

    private void TickBurrow(Enemy owner)
    {
        if (!burrowing)
        {
            phaseTimer += Time.deltaTime;
            float windup = Mathf.Max(0.05f, settings.burrowWindup);
            SetSink(Mathf.Clamp01(phaseTimer / windup));
            if (phaseTimer < windup) return;

            StartBurrow(owner, Flat(owner.PlayerTransform.position));
            phaseTimer = 0f;
            return;
        }

        phaseTimer += Time.deltaTime;
        float travelDuration = Mathf.Max(0.05f, settings.burrowUndergroundTime);
        float travelT = Mathf.Clamp01(phaseTimer / travelDuration);
        Vector3 undergroundPos = Vector3.Lerp(burrowOrigin, burrowDestination, travelT);
        owner.transform.position = undergroundPos;
        UpdateCrack(burrowOrigin, undergroundPos);

        if (travelT >= 1f)
        {
            ResetBurrow();
            // GDD flowchart: resurfacing resets bite and shot cooldowns and starts the 3 s burrow cooldown.
            biteCooldownTimer = 0f;
            shotCooldownTimer = 0f;
            burrowCooldownTimer = settings.burrowCooldown;
            actionFinished = true;
        }
    }

    private void StartBurrow(Enemy owner, Vector3 lockedPlayerPosition)
    {
        burrowing = true;
        burrowOrigin = owner.transform.position;
        if (owner.Agent != null && owner.Agent.enabled) owner.Agent.ResetPath();

        Vector3 clampedTarget = FlowerDecisionLogic.ClampBurrowTarget(burrowOrigin, lockedPlayerPosition, settings.burrowMaxHopDistance);
        burrowDestination = FindResurfacePoint(owner, clampedTarget);
        SetUndergroundInvulnerable(true);
        if (visualRoot != null) visualRoot.gameObject.SetActive(false);
        if (agent != null) agent.enabled = false;
        if (bodyCollider != null) bodyCollider.enabled = false;
    }

    private void ResetBurrow()
    {
        burrowing = false;

        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
        }

        SetUndergroundInvulnerable(false);
        if (bodyCollider != null) bodyCollider.enabled = true;

        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(true);
            SetSink(0f);
        }

        if (crack != null)
        {
            Destroy(crack.gameObject);
            crack = null;
        }
    }

    // Single toggle point so switching to Health.SetStateInvulnerability when PR #84 lands is a one-line change.
    private void SetUndergroundInvulnerable(bool active)
    {
        if (health != null) health.isInvulnerable = active;
    }

    private Vector3 FindResurfacePoint(Enemy owner, Vector3 target)
    {
        int attempts = Mathf.Max(1, settings.resurfaceAttempts);
        float minOffset = Mathf.Max(0.1f, settings.resurfaceMinOffset);
        float maxOffset = Mathf.Max(minOffset, settings.resurfaceMaxOffset);

        for (int i = 0; i < attempts; i++)
        {
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float radius = UnityEngine.Random.Range(minOffset, maxOffset);
            Vector3 candidate = new Vector3(
                target.x + Mathf.Cos(angle) * radius,
                owner.transform.position.y,
                target.z + Mathf.Sin(angle) * radius);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.6f, NavMesh.AllAreas) && IsClear(owner, hit.position))
            {
                return hit.position;
            }
        }

        if (NavMesh.SamplePosition(target, out NavMeshHit fallback, 2.5f, NavMesh.AllAreas))
        {
            return fallback.position;
        }

        return burrowOrigin;
    }

    private bool IsClear(Enemy owner, Vector3 position)
    {
        float radius = agent != null ? agent.radius * 0.8f : 0.4f;
        foreach (Collider other in Physics.OverlapSphere(position + Vector3.up * 0.6f, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (other.transform.IsChildOf(owner.transform)) continue;
            if (owner.PlayerTransform != null && other.transform.IsChildOf(owner.PlayerTransform)) continue;
            if (other.bounds.max.y <= position.y + 0.15f) continue;
            return false;
        }

        return true;
    }

    private void ShootNormal(Enemy owner)
    {
        Vector3 origin = shootPoint != null ? shootPoint.position : owner.transform.position + Vector3.up * 1.2f;
        Vector3 direction = Flat(owner.PlayerTransform.position) - Flat(owner.transform.position);
        if (direction.sqrMagnitude < 0.001f) direction = owner.transform.forward;

        EnemyStraightProjectile.Launch(
            origin,
            direction,
            owner,
            settings.projectileDamage,
            settings.projectileSpeed,
            settings.projectileRange,
            settings.projectileSize,
            projectileMaterial);
    }

    private void ShootPollinating(Enemy owner)
    {
        Vector3 origin = shootPoint != null ? shootPoint.position : owner.transform.position + Vector3.up * 1.2f;
        Vector3 target = FlowerDecisionLogic.ClampBurrowTarget(owner.transform.position, owner.PlayerTransform.position, settings.projectileRange);
        target.y = owner.transform.position.y;

        FlowerPollinatingSeed.Launch(
            origin,
            target,
            settings.pollinatingArcHeight,
            settings.pollinatingFlightTime,
            settings.pollinatingDamage,
            settings.projectileSize * 1.15f,
            projectileMaterial,
            owner,
            SpawnPollinatedFlower);
    }

    private void SpawnPollinatedFlower(Vector3 landingPosition)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        if (CountAliveSpawnedFlowers() >= settings.maxSpawnedFlowersAlive) return;

        Vector3 spawnPos = landingPosition;
        if (NavMesh.SamplePosition(landingPosition, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        GameObject template = spawnedFlowerPrefab != null ? spawnedFlowerPrefab : gameObject;
        GameObject spawned = Instantiate(template, spawnPos, Quaternion.identity, transform.parent);
        spawned.name = "Flower_Spawned";
        spawned.transform.localScale = Vector3.one * 0.85f;

        FlowerAttack childAttack = spawned.GetComponent<FlowerAttack>();
        if (childAttack != null)
        {
            childAttack.aliveTimer = 0f;
            childAttack.isGrown = false;
            childAttack.lastShotWasNormal = false;
            childAttack.ownSpawnedFlowers.Clear();
        }

        ownSpawnedFlowers.Add(spawned);
        OnEnemySpawned?.Invoke(spawned);
    }

    private int CountAliveSpawnedFlowers()
    {
        for (int i = ownSpawnedFlowers.Count - 1; i >= 0; i--)
        {
            GameObject child = ownSpawnedFlowers[i];
            if (child == null || !child.activeInHierarchy)
            {
                ownSpawnedFlowers.RemoveAt(i);
                continue;
            }

            Health childHealth = child.GetComponent<Health>();
            if (childHealth != null && childHealth.currentHealth <= 0f)
            {
                ownSpawnedFlowers.RemoveAt(i);
            }
        }

        return ownSpawnedFlowers.Count;
    }

    private void HandleDamageTaken(float amount)
    {
        tookDamage = true;
        isAware = true;
    }

    private void UpdateAwareness(float distanceToPlayer)
    {
        if (settings != null && distanceToPlayer <= settings.detectionRange)
        {
            isAware = true;
        }
    }

    private void UpdateCrack(Vector3 from, Vector3 to)
    {
        from = Flat(from, transform.position.y + 0.03f);
        to = Flat(to, from.y);
        Vector3 span = to - from;
        if (span.sqrMagnitude < 0.0001f) return;

        if (crack == null)
        {
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "Flower Crack";
            Destroy(strip.GetComponent<Collider>());
            if (crackMaterial != null) strip.GetComponent<Renderer>().sharedMaterial = crackMaterial;
            crack = strip.transform;
        }

        crack.position = from + span * 0.5f;
        crack.rotation = Quaternion.LookRotation(span);
        crack.localScale = new Vector3(settings.crackWidth, 0.02f, span.magnitude);
    }

    private void SetSink(float amount)
    {
        if (visualRoot != null)
        {
            visualRoot.localPosition = Vector3.down * (settings.sinkDepth * Mathf.Clamp01(amount));
        }
    }

    private static Vector3 Flat(Vector3 position, float y = 0f) => new Vector3(position.x, y, position.z);

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private static void Face(Enemy owner, Vector3 target)
    {
        Vector3 direction = target - owner.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            owner.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
