using UnityEngine;

public class ClockBomb : MonoBehaviour
{
    private Transform playerTransform;
    private Health playerHealth;
    private Renderer bombRenderer;
    private Transform fuseFill;
    private Material bombMaterial;
    private Vector3 start;
    private Vector3 target;
    private float damage;
    private float radius;
    private float fuseTime;
    private float flightTime;
    private float arcHeight;
    private float knockback;
    private float timer;
    private Color idleColor;
    private Color beepColor;
    private GameObject indicator;

    public static void Launch(Vector3 from, Vector3 to, Enemy owner, float damage, float fuseTime, float radius,
        float flightTime, float arcHeight, float knockback, float bombScale, Material bombMat, Material indicatorMat, Color beepColor)
    {
        GameObject bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bomb.name = "Clock Bomb";
        Destroy(bomb.GetComponent<Collider>());
        bomb.transform.SetPositionAndRotation(from, Quaternion.identity);
        bomb.transform.localScale = Vector3.one * bombScale;
        if (bombMat != null) bomb.GetComponent<Renderer>().sharedMaterial = bombMat;

        ClockBomb clockBomb = bomb.AddComponent<ClockBomb>();
        clockBomb.playerTransform = owner.PlayerTransform;
        clockBomb.playerHealth = owner.PlayerHealth;
        clockBomb.bombRenderer = bomb.GetComponent<Renderer>();
        clockBomb.start = from;
        clockBomb.target = to;
        clockBomb.damage = damage;
        clockBomb.fuseTime = fuseTime;
        clockBomb.radius = radius;
        clockBomb.flightTime = Mathf.Max(0.05f, flightTime);
        clockBomb.arcHeight = arcHeight;
        clockBomb.knockback = knockback;
        clockBomb.beepColor = beepColor;
        clockBomb.BuildIndicator(indicatorMat);
    }

    private void Start()
    {
        bombMaterial = bombRenderer.material;
        idleColor = bombMaterial.HasProperty("_BaseColor") ? bombMaterial.GetColor("_BaseColor") : bombMaterial.color;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < flightTime)
        {
            float t = timer / flightTime;
            transform.position = Vector3.Lerp(start, target, t) + Vector3.up * (arcHeight * 4f * t * (1f - t));
            return;
        }

        transform.position = target;
        if (!indicator.activeSelf) indicator.SetActive(true);

        float fuse = timer - flightTime;
        float progress = Mathf.Clamp01(fuse / Mathf.Max(fuseTime, 0.01f));
        fuseFill.localScale = new Vector3(radius * 2f * progress, 0.01f, radius * 2f * progress);

        float beepRate = Mathf.Lerp(4f, 16f, progress);
        SetColor(Mathf.Sin(fuse * beepRate * Mathf.PI) > 0f ? beepColor : idleColor);

        if (fuse >= fuseTime)
            Explode();
    }

    private void SetColor(Color color)
    {
        bombMaterial.color = color;
        if (bombMaterial.HasProperty("_BaseColor")) bombMaterial.SetColor("_BaseColor", color);
    }

    private void Explode()
    {
        if (playerTransform != null && playerHealth != null)
        {
            Vector3 offset = playerTransform.position - target;
            offset.y = 0f;
            if (offset.magnitude <= radius)
                playerHealth.TakeDamage(damage, target, knockback);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (indicator != null) Destroy(indicator);
        if (bombMaterial != null) Destroy(bombMaterial);
    }

    private void BuildIndicator(Material indicatorMat)
    {
        indicator = new GameObject("Clock Bomb AOE");
        indicator.transform.position = target + Vector3.up * 0.03f;
        indicator.SetActive(false);
        Transform ring = MakeDisc("Ring", indicatorMat, 1f);
        fuseFill = MakeDisc("Fill", indicatorMat, 0f);
        ring.SetParent(indicator.transform, false);
        fuseFill.SetParent(indicator.transform, false);
        fuseFill.localPosition = Vector3.up * 0.01f;
    }

    private Transform MakeDisc(string discName, Material mat, float scale)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = discName;
        Destroy(disc.GetComponent<Collider>());
        disc.transform.localScale = new Vector3(radius * 2f * scale, 0.01f, radius * 2f * scale);
        if (mat != null) disc.GetComponent<Renderer>().sharedMaterial = mat;
        return disc.transform;
    }
}
