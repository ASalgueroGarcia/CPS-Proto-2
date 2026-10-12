using UnityEngine;
public class EnemyVisualFeedback : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color hitColor = Color.white;
    [Min(0f)] [SerializeField] private float hitDuration = 0.12f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock propertyBlock;
    private Color baseColor;
    private Color telegraphColor;
    private float telegraphUntil;
    private float hitUntil;
    private bool initialized;

    public Renderer TargetRenderer => targetRenderer;
    public Color BaseColor => baseColor;

    private void Awake() => Initialize(targetRenderer);

    public void Initialize(Renderer renderer)
    {
        if (initialized) return;
        targetRenderer = renderer != null ? renderer : GetComponentInChildren<MeshRenderer>();
        if (targetRenderer == null || targetRenderer.sharedMaterial == null) return;

        Material material = targetRenderer.sharedMaterial;
        baseColor = material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId)
            : material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
        propertyBlock = new MaterialPropertyBlock();
        telegraphColor = baseColor;
        initialized = true;
        ApplyColor();
    }

    public void SetTelegraph(Color color, float duration = float.PositiveInfinity)
    {
        telegraphColor = color;
        telegraphUntil = Time.time + Mathf.Max(0f, duration);
        ApplyColor();
    }

    public void ClearTelegraph()
    {
        telegraphUntil = 0f;
        ApplyColor();
    }

    public void ShowHitFlash()
    {
        hitUntil = Time.time + hitDuration;
        ApplyColor();
    }

    private void LateUpdate() => ApplyColor();

    private Color GetDisplayColor()
    {
        Color cue = Time.time < telegraphUntil ? telegraphColor : baseColor;
        return Time.time < hitUntil ? Color.Lerp(cue, hitColor, 0.7f) : cue;
    }

    private void ApplyColor()
    {
        if (!initialized || targetRenderer == null) return;
        targetRenderer.GetPropertyBlock(propertyBlock);
        Color color = GetDisplayColor();
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable()
    {
        telegraphUntil = hitUntil = 0f;
        ApplyColor();
    }
}
