using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages the Caterpillar's modular URP sphere-chain visuals: head sphere with two button eyes,
/// trailing body spheres following a curved path via CaterpillarTrailMath, windup rearing arc,
/// ground slam telegraph disc, floor scatter during Split with head-only hitbox gating,
/// 0.5 s fly-back Reform, and detached scattered remains on split death.
/// </summary>
public class CaterpillarSegmentVisuals : MonoBehaviour
{
    private const string VisualsRootName = "Visuals";
    private const string HeadSegmentName = "HeadSegment";
    private const string BodySegmentPrefix = "BodySegment_";
    private const string EyeLeftName = "EyeLeft";
    private const string EyeRightName = "EyeRight";
    private const float ScatterBurstDuration = 0.15f;
    private const float ReformArcHeight = 0.2f;
    private const float ScatterBounceHeight = 0.3f;
    private const int DefaultEnemyLayer = 6;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Materials")]
    [SerializeField] private Material headMaterial;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material bodyAltMaterial;
    [SerializeField] private Material calmEyeMaterial;
    [SerializeField] private Material alertEyeMaterial;
    [SerializeField] private Material slamTelegraphMaterial;

    [Header("Hierarchy")]
    [SerializeField] private Transform segmentsRoot;

    [Header("Pose Tuning")]
    [Min(0f)]
    [SerializeField] private float rearHeightScale = 1f;
    [Min(0f)]
    [SerializeField] private float rearPullbackDistance = 0.45f;
    [Min(0.05f)]
    [SerializeField] private float eyeLocalScale = 0.24f;
    [Min(0f)]
    [SerializeField] private float telegraphGroundOffset = 0.03f;

    private readonly List<Transform> segments = new List<Transform>();
    private readonly List<MeshRenderer> segmentRenderers = new List<MeshRenderer>();
    private readonly List<SphereCollider> bodyColliders = new List<SphereCollider>();
    private readonly List<MeshRenderer> eyeRenderers = new List<MeshRenderer>();
    private readonly List<Material> runtimeMaterials = new List<Material>();

    private CaterpillarSettings settings;
    private CaterpillarTrailMath trail;
    private CaterpillarHeadHitbox headHitbox;
    private GameObject telegraphRoot;
    private Transform telegraphOuter;
    private Transform telegraphFill;
    private GameObject detachedRemainsRoot;

    private Vector3[] cachedWorldPositions;
    private Quaternion[] cachedWorldRotations;
    private Vector3[] slamStartPositions;
    private Vector3[] scatterStartPositions;
    private Vector3[] scatteredPositions;
    private Vector3[] reformStartPositions;
    private Vector3[] reformTargetPositions;

    private CaterpillarPhase lastPhase = CaterpillarPhase.Patrol;
    private bool hasLastPhase;
    private bool eyesAlert;
    private bool isHeadOnlyVulnerable;
    private bool initialized;
    private bool remainsDetached;

    public IReadOnlyList<Transform> Segments => segments;
    public IReadOnlyList<MeshRenderer> SegmentRenderers => segmentRenderers;
    public IReadOnlyList<SphereCollider> BodyColliders => bodyColliders;
    public IReadOnlyList<MeshRenderer> EyeRenderers => eyeRenderers;
    public MeshRenderer HeadRenderer => segmentRenderers.Count > 0 ? segmentRenderers[0] : null;
    public CaterpillarHeadHitbox HeadHitbox => headHitbox;
    public CaterpillarTrailMath Trail => trail;
    public IReadOnlyList<Vector3> ScatteredPositions => scatteredPositions;
    public bool AreEyesAlert => eyesAlert;
    public bool IsHeadOnlyVulnerable => isHeadOnlyVulnerable;
    public bool IsTelegraphVisible => telegraphRoot != null && telegraphRoot.activeSelf;
    public Vector3 TelegraphCenter => telegraphRoot != null ? telegraphRoot.transform.position : Vector3.zero;
    public GameObject DetachedRemainsRoot => detachedRemainsRoot;
    public Material CalmEyeMaterial => calmEyeMaterial;
    public Material AlertEyeMaterial => alertEyeMaterial;

    public void Initialize(CaterpillarSettings caterpillarSettings, Health sharedHealth, CaterpillarAttack ownerAttack)
    {
        settings = caterpillarSettings;
        if (settings == null)
            return;

        EnsureEyeMaterials();
        BuildOrBindSegments(sharedHealth, ownerAttack);
        AllocatePoseBuffers(settings.segmentCount);

        trail = new CaterpillarTrailMath(settings.segmentCount, settings.segmentSpacing);
        trail.Reset(transform.position, transform.forward);

        hasLastPhase = false;
        remainsDetached = false;
        detachedRemainsRoot = null;
        initialized = true;

        SetEyesAlert(false);
        SetSplitVulnerability(false);
        HideTelegraph();
        ApplyTrailPose(transform.position, transform.forward);
    }

    public void UpdateTrail(Vector3 rootPosition, Vector3 forward, float deltaTime)
    {
        if (!initialized || remainsDetached || trail == null)
            return;

        trail.UpdateSmoothed(rootPosition, settings.trailFollowSharpness, Mathf.Max(0f, deltaTime));
        ApplyTrailPose(rootPosition, forward);
    }

    public void ApplyPhaseVisuals(CaterpillarStateMachine stateMachine, Vector3 rootPosition, float deltaTime)
    {
        if (!initialized || stateMachine == null || remainsDetached)
            return;

        CaterpillarPhase phase = stateMachine.Phase;
        if (!hasLastPhase || phase != lastPhase)
        {
            HandlePhaseTransition(lastPhase, phase, stateMachine, rootPosition);
            lastPhase = phase;
            hasLastPhase = true;
        }

        SetEyesAlert(stateMachine.EyesAlert);

        if (phase == CaterpillarPhase.Dead)
        {
            SetAllCollidersEnabled(false);
            HideTelegraph();
            return;
        }

        SetSplitVulnerability(stateMachine.IsHeadOnlyVulnerable);

        switch (phase)
        {
            case CaterpillarPhase.Patrol:
            case CaterpillarPhase.Chase:
            case CaterpillarPhase.Hold:
                HideTelegraph();
                trail.UpdateSmoothed(rootPosition, settings.trailFollowSharpness, Mathf.Max(0f, deltaTime));
                ApplyTrailPose(rootPosition, stateMachine.TrackedDirection);
                break;

            case CaterpillarPhase.Windup:
                ApplyWindupPose(rootPosition, stateMachine.TrackedDirection, stateMachine.WindupProgress);
                Vector3 previewCenter = rootPosition + stateMachine.TrackedDirection * settings.slamCenterOffset;
                UpdateTelegraph(previewCenter, stateMachine.WindupProgress);
                break;

            case CaterpillarPhase.Slam:
                ApplySlamPose(rootPosition, stateMachine.LockedSlamDirection, stateMachine.SlamImpactCenter, stateMachine.SlamProgress);
                UpdateTelegraph(stateMachine.SlamImpactCenter, 1f);
                break;

            case CaterpillarPhase.Split:
                HideTelegraph();
                ApplySplitPose(stateMachine.SplitProgress);
                break;

            case CaterpillarPhase.Reform:
                HideTelegraph();
                ApplyReformPose(stateMachine.TrackedDirection, stateMachine.ReformProgress);
                break;
        }
    }

    /// <summary>Reapplies cached world positions after NavMeshAgent movement so trailing spheres never drag rigidly.</summary>
    public void ReapplyWorldTransforms()
    {
        if (!initialized || remainsDetached || cachedWorldPositions == null)
            return;

        int count = Mathf.Min(segments.Count, cachedWorldPositions.Length);
        for (int i = 0; i < count; i++)
        {
            if (segments[i] == null)
                continue;

            segments[i].SetPositionAndRotation(cachedWorldPositions[i], cachedWorldRotations[i]);
        }
    }

    public void SetEyesAlert(bool alert)
    {
        eyesAlert = alert;
        EnsureEyeMaterials();
        Material targetMaterial = alert ? alertEyeMaterial : calmEyeMaterial;

        for (int i = 0; i < eyeRenderers.Count; i++)
        {
            if (eyeRenderers[i] != null && targetMaterial != null)
                eyeRenderers[i].sharedMaterial = targetMaterial;
        }
    }

    public void SetSplitVulnerability(bool headOnly)
    {
        isHeadOnlyVulnerable = headOnly;

        if (headHitbox != null)
            headHitbox.SetActive(true);

        bool bodyEnabled = !headOnly;
        for (int i = 0; i < bodyColliders.Count; i++)
        {
            if (bodyColliders[i] != null)
                bodyColliders[i].enabled = bodyEnabled;
        }
    }

    public void SetAllCollidersEnabled(bool enabled)
    {
        if (headHitbox != null)
            headHitbox.SetActive(enabled);

        for (int i = 0; i < bodyColliders.Count; i++)
        {
            if (bodyColliders[i] != null)
                bodyColliders[i].enabled = enabled;
        }
    }

    public void HandleDeath(bool diedWhileSplit, Transform remainsParent)
    {
        HideTelegraph();
        SetEyesAlert(false);
        SetAllCollidersEnabled(false);

        if (diedWhileSplit)
            DetachScatteredRemains(remainsParent);
    }

    /// <summary>
    /// Detaches scattered sphere segments onto remainsParent (typically the room's WaveManager)
    /// so pieces stay on the floor after the Caterpillar dies while Split and clean up on room unload.
    /// </summary>
    public GameObject DetachScatteredRemains(Transform remainsParent)
    {
        if (remainsDetached)
            return detachedRemainsRoot;

        remainsDetached = true;
        detachedRemainsRoot = new GameObject($"{gameObject.name}_ScatteredRemains");
        detachedRemainsRoot.transform.SetParent(remainsParent, worldPositionStays: true);

        for (int i = 0; i < segments.Count; i++)
        {
            Transform segment = segments[i];
            if (segment == null)
                continue;

            Vector3 groundedPosition = segment.position;
            if (scatteredPositions != null && i < scatteredPositions.Length)
                groundedPosition.y = scatteredPositions[i].y;

            segment.position = groundedPosition;
            segment.SetParent(detachedRemainsRoot.transform, worldPositionStays: true);

            SphereCollider collider = segment.GetComponent<SphereCollider>();
            if (collider != null)
                collider.enabled = false;
        }

        return detachedRemainsRoot;
    }

    public void CleanupTelegraph()
    {
        if (telegraphRoot != null)
        {
            DestroySafe(telegraphRoot);
            telegraphRoot = null;
            telegraphOuter = null;
            telegraphFill = null;
        }
    }

    private void OnDestroy()
    {
        CleanupTelegraph();

        for (int i = 0; i < runtimeMaterials.Count; i++)
        {
            if (runtimeMaterials[i] != null)
                DestroySafe(runtimeMaterials[i]);
        }

        runtimeMaterials.Clear();
    }

    private void HandlePhaseTransition(CaterpillarPhase fromPhase, CaterpillarPhase toPhase, CaterpillarStateMachine stateMachine, Vector3 rootPosition)
    {
        switch (toPhase)
        {
            case CaterpillarPhase.Slam:
                CaptureCurrentPositions(slamStartPositions);
                break;

            case CaterpillarPhase.Split:
                CaptureCurrentPositions(scatterStartPositions);
                ComputeScatteredTargets(stateMachine.SlamImpactCenter, stateMachine.LockedSlamDirection, rootPosition.y);
                break;

            case CaterpillarPhase.Reform:
                CaptureCurrentPositions(reformStartPositions);
                trail.Reset(rootPosition, stateMachine.TrackedDirection);
                for (int i = 0; i < settings.segmentCount; i++)
                {
                    Vector3 pos = trail.SmoothedPositions[i];
                    pos.y = rootPosition.y + GetSegmentRadius(i);
                    reformTargetPositions[i] = pos;
                }
                break;
        }
    }

    private void BuildOrBindSegments(Health sharedHealth, CaterpillarAttack ownerAttack)
    {
        if (segmentsRoot == null)
        {
            Transform existingRoot = transform.Find(VisualsRootName);
            if (existingRoot != null)
            {
                segmentsRoot = existingRoot;
            }
            else
            {
                GameObject rootObj = new GameObject(VisualsRootName);
                rootObj.transform.SetParent(transform, false);
                segmentsRoot = rootObj.transform;
            }
        }

        int enemyLayer = ResolveEnemyLayer();
        segments.Clear();
        segmentRenderers.Clear();
        bodyColliders.Clear();
        eyeRenderers.Clear();

        Transform head = FindOrCreateSegment(HeadSegmentName, isHead: true, segmentIndex: 0, enemyLayer);
        ConfigureSegment(head, settings.headRadius, headMaterial, enemyLayer);

        SphereCollider headCollider = EnsureTriggerSphereCollider(head.gameObject);
        headHitbox = head.GetComponent<CaterpillarHeadHitbox>();
        if (headHitbox == null)
            headHitbox = head.gameObject.AddComponent<CaterpillarHeadHitbox>();
        headHitbox.Initialize(sharedHealth, headCollider, ownerAttack);

        segments.Add(head);
        segmentRenderers.Add(head.GetComponent<MeshRenderer>());

        BindOrCreateEyes(head, enemyLayer);

        int requiredBodyCount = Mathf.Max(1, settings.segmentCount - 1);
        for (int i = 1; i <= requiredBodyCount; i++)
        {
            Transform body = FindOrCreateSegment($"{BodySegmentPrefix}{i}", isHead: false, segmentIndex: i, enemyLayer);
            Material mat = (i % 2 == 1)
                ? bodyMaterial
                : (bodyAltMaterial != null ? bodyAltMaterial : bodyMaterial);
            ConfigureSegment(body, settings.segmentRadius, mat, enemyLayer);

            SphereCollider bodyCollider = EnsureTriggerSphereCollider(body.gameObject);
            segments.Add(body);
            segmentRenderers.Add(body.GetComponent<MeshRenderer>());
            bodyColliders.Add(bodyCollider);
        }

        RemoveExtraBodySegments(requiredBodyCount);
    }

    private Transform FindOrCreateSegment(string segmentName, bool isHead, int segmentIndex, int enemyLayer)
    {
        Transform existing = segmentsRoot.Find(segmentName);
        if (existing != null)
            return existing;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = segmentName;
        sphere.layer = enemyLayer;
        sphere.tag = "Enemy";
        sphere.transform.SetParent(segmentsRoot, false);

        float radius = isHead ? settings.headRadius : settings.segmentRadius;
        sphere.transform.localPosition = new Vector3(0f, radius, -segmentIndex * settings.segmentSpacing);
        return sphere.transform;
    }

    private void ConfigureSegment(Transform segment, float radius, Material material, int enemyLayer)
    {
        segment.gameObject.layer = enemyLayer;
        segment.gameObject.tag = "Enemy";
        segment.localScale = Vector3.one * (radius * 2f);

        MeshRenderer renderer = segment.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;
    }

    private void BindOrCreateEyes(Transform head, int enemyLayer)
    {
        Transform leftEye = FindOrCreateEye(head, EyeLeftName, new Vector3(-0.2f, 0.16f, 0.4f), enemyLayer);
        Transform rightEye = FindOrCreateEye(head, EyeRightName, new Vector3(0.2f, 0.16f, 0.4f), enemyLayer);

        MeshRenderer leftRenderer = leftEye.GetComponent<MeshRenderer>();
        MeshRenderer rightRenderer = rightEye.GetComponent<MeshRenderer>();
        if (leftRenderer != null) eyeRenderers.Add(leftRenderer);
        if (rightRenderer != null) eyeRenderers.Add(rightRenderer);
    }

    private Transform FindOrCreateEye(Transform head, string eyeName, Vector3 localPosition, int enemyLayer)
    {
        Transform eye = head.Find(eyeName);
        if (eye == null)
        {
            GameObject eyeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeObject.name = eyeName;
            eyeObject.layer = enemyLayer;
            DestroySafe(eyeObject.GetComponent<Collider>());
            eye = eyeObject.transform;
            eye.SetParent(head, false);
            eye.localPosition = localPosition;
            eye.localScale = Vector3.one * eyeLocalScale;
        }
        else
        {
            Collider existingCollider = eye.GetComponent<Collider>();
            if (existingCollider != null)
                DestroySafe(existingCollider);
        }

        return eye;
    }

    private void RemoveExtraBodySegments(int requiredBodyCount)
    {
        var extras = new List<GameObject>();
        for (int i = 0; i < segmentsRoot.childCount; i++)
        {
            Transform child = segmentsRoot.GetChild(i);
            if (!child.name.StartsWith(BodySegmentPrefix))
                continue;

            if (int.TryParse(child.name.Substring(BodySegmentPrefix.Length), out int index) && index > requiredBodyCount)
                extras.Add(child.gameObject);
        }

        for (int i = 0; i < extras.Count; i++)
            DestroySafe(extras[i]);
    }

    private void AllocatePoseBuffers(int count)
    {
        cachedWorldPositions = new Vector3[count];
        cachedWorldRotations = new Quaternion[count];
        slamStartPositions = new Vector3[count];
        scatterStartPositions = new Vector3[count];
        scatteredPositions = new Vector3[count];
        reformStartPositions = new Vector3[count];
        reformTargetPositions = new Vector3[count];
    }

    private void ApplyTrailPose(Vector3 rootPosition, Vector3 forward)
    {
        IReadOnlyList<Vector3> smoothed = trail.SmoothedPositions;
        Vector3 headForward = NormalizeHorizontal(forward, Vector3.forward);

        for (int i = 0; i < segments.Count; i++)
        {
            Vector3 pos = smoothed[i];
            pos.y = rootPosition.y + GetSegmentRadius(i);

            Vector3 lookDir = i == 0 ? headForward : (smoothed[i - 1] - smoothed[i]);
            lookDir.y = 0f;
            Quaternion rot = lookDir.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(lookDir.normalized, Vector3.up)
                : Quaternion.LookRotation(headForward, Vector3.up);

            SetSegmentPose(i, pos, rot);
        }
    }

    private void ApplyWindupPose(Vector3 rootPosition, Vector3 trackedDirection, float windupProgress)
    {
        float rearT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(windupProgress));
        Vector3 forward = NormalizeHorizontal(trackedDirection, Vector3.forward);
        IReadOnlyList<Vector3> basePositions = trail.SmoothedPositions;

        int count = segments.Count;
        float maxLift = settings.segmentSpacing * Mathf.Max(1, count - 1) * 0.55f * rearHeightScale;

        for (int i = 0; i < count; i++)
        {
            Vector3 flatBase = basePositions[i];
            flatBase.y = rootPosition.y + GetSegmentRadius(i);

            float frontWeight = count > 1 ? 1f - (i / (float)(count - 1)) : 1f;
            float liftCurve = Mathf.Pow(frontWeight, 1.35f);
            float pullbackCurve = Mathf.Sin(frontWeight * Mathf.PI * 0.5f);

            Vector3 rearedTarget = flatBase
                + Vector3.up * (liftCurve * maxLift)
                - forward * (pullbackCurve * rearPullbackDistance);

            Vector3 pos = Vector3.Lerp(flatBase, rearedTarget, rearT);

            Quaternion flatRot = Quaternion.LookRotation(forward, Vector3.up);
            Quaternion rearedRot = flatRot * Quaternion.Euler(-25f * frontWeight * rearT, 0f, 0f);
            SetSegmentPose(i, pos, rearedRot);
        }
    }

    private void ApplySlamPose(Vector3 rootPosition, Vector3 slamDirection, Vector3 impactCenter, float slamProgress)
    {
        float t = Mathf.Clamp01(slamProgress);
        float fallEase = t * t;
        Vector3 forward = NormalizeHorizontal(slamDirection, Vector3.forward);

        int count = segments.Count;
        for (int i = 0; i < count; i++)
        {
            float frontWeight = count > 1 ? 1f - (i / (float)(count - 1)) : 1f;
            Vector3 slamTarget = Vector3.Lerp(
                rootPosition - forward * (settings.segmentSpacing * 0.5f),
                impactCenter,
                frontWeight);
            slamTarget.y = rootPosition.y + GetSegmentRadius(i);

            Vector3 pos = Vector3.Lerp(slamStartPositions[i], slamTarget, fallEase);
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(20f * frontWeight * fallEase, 0f, 0f);
            SetSegmentPose(i, pos, rot);
        }
    }

    private void ApplySplitPose(float splitProgress)
    {
        float splitElapsed = Mathf.Clamp01(splitProgress) * Mathf.Max(0.001f, settings.splitDuration);
        float burstT = Mathf.Clamp01(splitElapsed / ScatterBurstDuration);
        float eased = 1f - (1f - burstT) * (1f - burstT);
        float hop = Mathf.Sin(burstT * Mathf.PI) * ScatterBounceHeight;

        for (int i = 0; i < segments.Count; i++)
        {
            Vector3 pos = Vector3.Lerp(scatterStartPositions[i], scatteredPositions[i], eased);
            pos.y += hop;

            Quaternion rot = cachedWorldRotations[i];
            SetSegmentPose(i, pos, rot);
        }
    }

    private void ApplyReformPose(Vector3 trackedDirection, float reformProgress)
    {
        float progress = Mathf.Clamp01(reformProgress);
        float lift = Mathf.Sin(progress * Mathf.PI) * ReformArcHeight;
        Vector3 forward = NormalizeHorizontal(trackedDirection, Vector3.forward);
        Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);

        for (int i = 0; i < segments.Count; i++)
        {
            Vector3 pos = CaterpillarTrailMath.EvaluateReformPosition(reformStartPositions[i], reformTargetPositions[i], progress);
            pos.y += lift;
            SetSegmentPose(i, pos, targetRot);
        }
    }

    private void ComputeScatteredTargets(Vector3 impactCenter, Vector3 lockedDirection, float fallbackFloorY)
    {
        int count = segments.Count;
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = CaterpillarTrailMath.ComputeScatterOffset(i, count, settings.scatterRadius, lockedDirection);
            Vector3 candidate = new Vector3(impactCenter.x + offset.x, fallbackFloorY, impactCenter.z + offset.z);

            if (NavMesh.SamplePosition(impactCenter, out NavMeshHit startHit, 1.5f, NavMesh.AllAreas))
            {
                if (NavMesh.Raycast(startHit.position, candidate, out NavMeshHit wallHit, NavMesh.AllAreas))
                    candidate = wallHit.position;
                else if (NavMesh.SamplePosition(candidate, out NavMeshHit endHit, 1.5f, NavMesh.AllAreas))
                    candidate = endHit.position;
            }

            float floorY = ResolveFloorHeight(candidate, fallbackFloorY);
            scatteredPositions[i] = new Vector3(candidate.x, floorY + GetSegmentRadius(i), candidate.z);
        }
    }

    private float ResolveFloorHeight(Vector3 position, float fallbackFloorY)
    {
        Vector3 rayOrigin = new Vector3(position.x, fallbackFloorY + 2f, position.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.transform.IsChildOf(transform))
                return hit.point.y;
        }

        return fallbackFloorY;
    }

    private void CaptureCurrentPositions(Vector3[] destination)
    {
        int count = Mathf.Min(segments.Count, destination.Length);
        for (int i = 0; i < count; i++)
            destination[i] = segments[i] != null ? segments[i].position : transform.position;
    }

    private void SetSegmentPose(int index, Vector3 worldPosition, Quaternion worldRotation)
    {
        cachedWorldPositions[index] = worldPosition;
        cachedWorldRotations[index] = worldRotation;

        if (segments[index] != null)
            segments[index].SetPositionAndRotation(worldPosition, worldRotation);
    }

    private void UpdateTelegraph(Vector3 center, float fillProgress)
    {
        EnsureTelegraphVisual();

        float floorY = ResolveFloorHeight(center, transform.position.y);
        telegraphRoot.transform.position = new Vector3(center.x, floorY + telegraphGroundOffset, center.z);
        telegraphRoot.SetActive(true);

        float diameter = Mathf.Max(0.1f, settings.slamImpactRadius * 2f);
        telegraphOuter.localScale = new Vector3(diameter, 0.01f, diameter);

        float clampedFill = Mathf.Clamp01(fillProgress);
        telegraphFill.localScale = new Vector3(diameter * clampedFill, 0.012f, diameter * clampedFill);
    }

    private void HideTelegraph()
    {
        if (telegraphRoot != null)
            telegraphRoot.SetActive(false);
    }

    private void EnsureTelegraphVisual()
    {
        if (telegraphRoot != null)
            return;

        telegraphRoot = new GameObject("CaterpillarSlamTelegraph");
        telegraphRoot.SetActive(false);

        telegraphOuter = CreateTelegraphDisc("OuterArea", slamTelegraphMaterial);
        telegraphOuter.SetParent(telegraphRoot.transform, false);

        telegraphFill = CreateTelegraphDisc("WindupFill", slamTelegraphMaterial);
        telegraphFill.SetParent(telegraphRoot.transform, false);
        telegraphFill.localPosition = Vector3.up * 0.005f;
    }

    private Transform CreateTelegraphDisc(string discName, Material material)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = discName;
        DestroySafe(disc.GetComponent<Collider>());

        MeshRenderer renderer = disc.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (material != null)
                renderer.sharedMaterial = material;
        }

        return disc.transform;
    }

    private void EnsureEyeMaterials()
    {
        if (calmEyeMaterial == null)
            calmEyeMaterial = CreateFallbackMaterial("CaterpillarEyeCalmRuntime", new Color(0.08f, 0.08f, 0.11f, 1f));

        if (alertEyeMaterial == null)
            alertEyeMaterial = CreateFallbackMaterial("CaterpillarEyeAlertRuntime", new Color(1f, 0.22f, 0.08f, 1f));
    }

    private Material CreateFallbackMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Sprites/Default")
            ?? Shader.Find("Hidden/InternalErrorShader");
        if (shader == null)
            return null;

        Material mat = new Material(shader);
        mat.name = materialName;
        if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, color);
        if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, color);
        runtimeMaterials.Add(mat);
        return mat;
    }

    private float GetSegmentRadius(int index) => index == 0 ? settings.headRadius : settings.segmentRadius;

    private static SphereCollider EnsureTriggerSphereCollider(GameObject target)
    {
        SphereCollider collider = target.GetComponent<SphereCollider>();
        if (collider == null)
            collider = target.AddComponent<SphereCollider>();

        collider.radius = 0.5f;
        collider.center = Vector3.zero;
        collider.isTrigger = true;
        return collider;
    }

    private static int ResolveEnemyLayer()
    {
        int layer = LayerMask.NameToLayer("Enemy");
        return layer != -1 ? layer : DefaultEnemyLayer;
    }

    private static Vector3 NormalizeHorizontal(Vector3 vector, Vector3 fallback)
    {
        vector.y = 0f;
        if (vector.sqrMagnitude > 0.0001f)
            return vector.normalized;

        fallback.y = 0f;
        return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
    }

    private static void DestroySafe(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
