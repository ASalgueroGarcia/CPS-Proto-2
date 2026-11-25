using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Blinks the player's character renderers between semi-transparent and opaque after a pit respawn,
/// switching URP materials cleanly to alpha-blended transparency and restoring original state when finished.
/// </summary>
public class PlayerRespawnBlink : MonoBehaviour
{
    private const string TransparentSurfaceKeyword = "_SURFACE_TYPE_TRANSPARENT";
    private const string AlphaPremultiplyKeyword = "_ALPHAPREMULTIPLY_ON";
    private const string AlphaModulateKeyword = "_ALPHAMODULATE_ON";
    private const string AlphaBlendKeyword = "_ALPHABLEND_ON";
    private const string AlphaTestKeyword = "_ALPHATEST_ON";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int SrcBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
    private static readonly int DstBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");

    private struct MaterialSnapshot
    {
        public Renderer renderer;
        public Material material;
        public bool hasBaseColor;
        public float originalBaseAlpha;
        public bool hasColor;
        public float originalColorAlpha;
        public bool hasTintColor;
        public float originalTintAlpha;
        public bool hasSurface;
        public float surface;
        public bool hasBlend;
        public float blend;
        public bool hasSrcBlend;
        public float srcBlend;
        public bool hasDstBlend;
        public float dstBlend;
        public bool hasSrcBlendAlpha;
        public float srcBlendAlpha;
        public bool hasDstBlendAlpha;
        public float dstBlendAlpha;
        public bool hasZWrite;
        public float zWrite;
        public bool hasAlphaClip;
        public float alphaClip;
        public int renderQueue;
        public string renderType;
        public bool kwSurfaceTransparent;
        public bool kwAlphaPremultiply;
        public bool kwAlphaModulate;
        public bool kwAlphaBlend;
        public bool kwAlphaTest;
    }

    private struct EditModeRendererBackup
    {
        public Renderer renderer;
        public Material[] originalSharedMaterials;
        public Material[] clonedMaterials;
    }

    private readonly List<MaterialSnapshot> snapshots = new List<MaterialSnapshot>();
    private readonly List<EditModeRendererBackup> editModeBackups = new List<EditModeRendererBackup>();

    private Player player;
    private bool captured;
    private bool isBlinking;
    private float blinkElapsed;
    private float blinkDuration;
    private float blinkInterval;
    private float blinkMinAlpha;
    private float currentAlpha = 1f;

    public bool IsBlinking => isBlinking;
    public float CurrentAlpha => currentAlpha;

    public void Initialize(Player owner)
    {
        player = owner;
    }

    public void CaptureIfNeeded()
    {
        if (captured)
            return;

        snapshots.Clear();
        editModeBackups.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null || (!(targetRenderer is SkinnedMeshRenderer) && !(targetRenderer is MeshRenderer)))
                continue;

            Material[] shared = targetRenderer.sharedMaterials;
            if (shared == null || shared.Length == 0)
                continue;

            Material[] workingMaterials;
            if (Application.isPlaying)
            {
                workingMaterials = targetRenderer.materials;
            }
            else
            {
                workingMaterials = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] != null)
                        workingMaterials[i] = new Material(shared[i]);
                }

                targetRenderer.sharedMaterials = workingMaterials;
                editModeBackups.Add(new EditModeRendererBackup
                {
                    renderer = targetRenderer,
                    originalSharedMaterials = shared,
                    clonedMaterials = workingMaterials
                });
            }

            for (int i = 0; i < workingMaterials.Length; i++)
            {
                Material mat = workingMaterials[i];
                if (mat == null)
                    continue;

                snapshots.Add(CreateSnapshot(targetRenderer, mat));
            }
        }

        captured = snapshots.Count > 0;
    }

    public void StartBlink(float duration, float interval, float minAlpha)
    {
        if (duration <= 0f)
        {
            StopBlink();
            return;
        }

        CaptureIfNeeded();
        if (snapshots.Count == 0)
            return;

        blinkDuration = duration;
        blinkInterval = Mathf.Max(0.01f, interval);
        blinkMinAlpha = Mathf.Clamp01(minAlpha);
        blinkElapsed = 0f;
        isBlinking = true;

        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i].material != null)
                EnableTransparentBlending(snapshots[i].material);
        }

        currentAlpha = PitFallRules.EvaluateBlinkAlpha(0f, blinkDuration, blinkInterval, blinkMinAlpha);
        ApplyAlphaToAll(currentAlpha);
    }

    public void TickBlink(float deltaTime)
    {
        if (!isBlinking)
            return;

        blinkElapsed += Mathf.Max(0f, deltaTime);
        if (blinkElapsed >= blinkDuration)
        {
            StopBlink();
            return;
        }

        currentAlpha = PitFallRules.EvaluateBlinkAlpha(blinkElapsed, blinkDuration, blinkInterval, blinkMinAlpha);
        ApplyAlphaToAll(currentAlpha);
    }

    public void StopBlink()
    {
        if (!isBlinking && editModeBackups.Count == 0)
        {
            currentAlpha = 1f;
            return;
        }

        isBlinking = false;
        blinkElapsed = 0f;
        currentAlpha = 1f;

        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i].material != null)
                RestoreSnapshot(snapshots[i]);
        }

        if (!Application.isPlaying && editModeBackups.Count > 0)
        {
            CleanupEditModeClones();
        }
    }

    private void LateUpdate()
    {
        if (isBlinking)
            TickBlink(Time.deltaTime);
    }

    private void OnDisable()
    {
        StopBlink();
    }

    private void OnDestroy()
    {
        StopBlink();

        if (!Application.isPlaying && editModeBackups.Count > 0)
            CleanupEditModeClones();
    }

    private void CleanupEditModeClones()
    {
        for (int i = 0; i < editModeBackups.Count; i++)
        {
            EditModeRendererBackup backup = editModeBackups[i];
            if (backup.renderer != null)
                backup.renderer.sharedMaterials = backup.originalSharedMaterials;

            if (backup.clonedMaterials != null)
            {
                for (int m = 0; m < backup.clonedMaterials.Length; m++)
                {
                    if (backup.clonedMaterials[m] != null)
                        DestroyImmediate(backup.clonedMaterials[m]);
                }
            }
        }

        editModeBackups.Clear();
        snapshots.Clear();
        captured = false;
    }

    private void ApplyAlphaToAll(float alpha)
    {
        for (int i = 0; i < snapshots.Count; i++)
        {
            MaterialSnapshot snap = snapshots[i];
            if (snap.material == null)
                continue;

            if (snap.hasBaseColor)
            {
                Color c = snap.material.GetColor(BaseColorId);
                c.a = snap.originalBaseAlpha * alpha;
                snap.material.SetColor(BaseColorId, c);
            }

            if (snap.hasColor)
            {
                Color c = snap.material.GetColor(ColorId);
                c.a = snap.originalColorAlpha * alpha;
                snap.material.SetColor(ColorId, c);
            }

            if (snap.hasTintColor)
            {
                Color c = snap.material.GetColor(TintColorId);
                c.a = snap.originalTintAlpha * alpha;
                snap.material.SetColor(TintColorId, c);
            }
        }
    }

    private static MaterialSnapshot CreateSnapshot(Renderer targetRenderer, Material mat)
    {
        MaterialSnapshot snap = new MaterialSnapshot
        {
            renderer = targetRenderer,
            material = mat,
            hasBaseColor = mat.HasProperty(BaseColorId),
            hasColor = mat.HasProperty(ColorId),
            hasTintColor = mat.HasProperty(TintColorId),
            hasSurface = mat.HasProperty(SurfaceId),
            hasBlend = mat.HasProperty(BlendId),
            hasSrcBlend = mat.HasProperty(SrcBlendId),
            hasDstBlend = mat.HasProperty(DstBlendId),
            hasSrcBlendAlpha = mat.HasProperty(SrcBlendAlphaId),
            hasDstBlendAlpha = mat.HasProperty(DstBlendAlphaId),
            hasZWrite = mat.HasProperty(ZWriteId),
            hasAlphaClip = mat.HasProperty(AlphaClipId),
            renderQueue = mat.renderQueue,
            renderType = mat.GetTag("RenderType", false, ""),
            kwSurfaceTransparent = mat.IsKeywordEnabled(TransparentSurfaceKeyword),
            kwAlphaPremultiply = mat.IsKeywordEnabled(AlphaPremultiplyKeyword),
            kwAlphaModulate = mat.IsKeywordEnabled(AlphaModulateKeyword),
            kwAlphaBlend = mat.IsKeywordEnabled(AlphaBlendKeyword),
            kwAlphaTest = mat.IsKeywordEnabled(AlphaTestKeyword)
        };

        snap.originalBaseAlpha = snap.hasBaseColor ? NormalizeOriginalAlpha(mat.GetColor(BaseColorId).a) : 1f;
        snap.originalColorAlpha = snap.hasColor ? NormalizeOriginalAlpha(mat.GetColor(ColorId).a) : 1f;
        snap.originalTintAlpha = snap.hasTintColor ? NormalizeOriginalAlpha(mat.GetColor(TintColorId).a) : 1f;

        if (snap.hasSurface) snap.surface = mat.GetFloat(SurfaceId);
        if (snap.hasBlend) snap.blend = mat.GetFloat(BlendId);
        if (snap.hasSrcBlend) snap.srcBlend = mat.GetFloat(SrcBlendId);
        if (snap.hasDstBlend) snap.dstBlend = mat.GetFloat(DstBlendId);
        if (snap.hasSrcBlendAlpha) snap.srcBlendAlpha = mat.GetFloat(SrcBlendAlphaId);
        if (snap.hasDstBlendAlpha) snap.dstBlendAlpha = mat.GetFloat(DstBlendAlphaId);
        if (snap.hasZWrite) snap.zWrite = mat.GetFloat(ZWriteId);
        if (snap.hasAlphaClip) snap.alphaClip = mat.GetFloat(AlphaClipId);

        return snap;
    }

    // Opaque materials in the project sometimes carry alpha 0 from inspector color tints (e.g. combo colors).
    private static float NormalizeOriginalAlpha(float capturedAlpha)
    {
        return capturedAlpha > 0.01f ? capturedAlpha : 1f;
    }

    private static void EnableTransparentBlending(Material mat)
    {
        if (mat.HasProperty(SurfaceId)) mat.SetFloat(SurfaceId, 1f);
        if (mat.HasProperty(BlendId)) mat.SetFloat(BlendId, 0f);
        if (mat.HasProperty(SrcBlendId)) mat.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
        if (mat.HasProperty(DstBlendId)) mat.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty(SrcBlendAlphaId)) mat.SetFloat(SrcBlendAlphaId, (float)BlendMode.One);
        if (mat.HasProperty(DstBlendAlphaId)) mat.SetFloat(DstBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty(ZWriteId)) mat.SetFloat(ZWriteId, 0f);
        if (mat.HasProperty(AlphaClipId)) mat.SetFloat(AlphaClipId, 0f);

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword(TransparentSurfaceKeyword);
        mat.DisableKeyword(AlphaPremultiplyKeyword);
        mat.DisableKeyword(AlphaModulateKeyword);
        mat.DisableKeyword(AlphaTestKeyword);
        mat.EnableKeyword(AlphaBlendKeyword);
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void RestoreSnapshot(MaterialSnapshot snap)
    {
        Material mat = snap.material;
        if (snap.hasSurface) mat.SetFloat(SurfaceId, snap.surface);
        if (snap.hasBlend) mat.SetFloat(BlendId, snap.blend);
        if (snap.hasSrcBlend) mat.SetFloat(SrcBlendId, snap.srcBlend);
        if (snap.hasDstBlend) mat.SetFloat(DstBlendId, snap.dstBlend);
        if (snap.hasSrcBlendAlpha) mat.SetFloat(SrcBlendAlphaId, snap.srcBlendAlpha);
        if (snap.hasDstBlendAlpha) mat.SetFloat(DstBlendAlphaId, snap.dstBlendAlpha);
        if (snap.hasZWrite) mat.SetFloat(ZWriteId, snap.zWrite);
        if (snap.hasAlphaClip) mat.SetFloat(AlphaClipId, snap.alphaClip);

        mat.SetOverrideTag("RenderType", snap.renderType);
        SetKeyword(mat, TransparentSurfaceKeyword, snap.kwSurfaceTransparent);
        SetKeyword(mat, AlphaPremultiplyKeyword, snap.kwAlphaPremultiply);
        SetKeyword(mat, AlphaModulateKeyword, snap.kwAlphaModulate);
        SetKeyword(mat, AlphaBlendKeyword, snap.kwAlphaBlend);
        SetKeyword(mat, AlphaTestKeyword, snap.kwAlphaTest);
        mat.renderQueue = snap.renderQueue;

        if (snap.hasBaseColor)
        {
            Color c = mat.GetColor(BaseColorId);
            c.a = snap.originalBaseAlpha;
            mat.SetColor(BaseColorId, c);
        }

        if (snap.hasColor)
        {
            Color c = mat.GetColor(ColorId);
            c.a = snap.originalColorAlpha;
            mat.SetColor(ColorId, c);
        }

        if (snap.hasTintColor)
        {
            Color c = mat.GetColor(TintColorId);
            c.a = snap.originalTintAlpha;
            mat.SetColor(TintColorId, c);
        }
    }

    private static void SetKeyword(Material mat, string keyword, bool enabled)
    {
        if (enabled)
            mat.EnableKeyword(keyword);
        else
            mat.DisableKeyword(keyword);
    }
}
