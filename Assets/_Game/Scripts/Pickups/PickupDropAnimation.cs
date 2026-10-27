using System.Collections;
using UnityEngine;

public class PickupDropAnimation : MonoBehaviour
{
    [SerializeField] private float dropHeight = 6f;
    [SerializeField] private float fallTime = 0.8f;
    [SerializeField] private float bounceHeight = 0.25f;
    [SerializeField] private float bounceTime = 0.3f;
    [SerializeField] private float spinSpeed = 900f;

    [Header("Shadow")]
    [SerializeField] private Material shadowMaterial;
    [SerializeField] private float shadowSize = 0.8f;

    private static readonly int baseColor = Shader.PropertyToID("_BaseColor");
    private Renderer shadow;

    public void Drop(Vector3 restPosition, float groundY)
    {
        StartCoroutine(Fall(restPosition, groundY));
    }

    private IEnumerator Fall(Vector3 rest, float groundY)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        Quaternion upright = transform.rotation;
        Vector3 spinAxis = Random.onUnitSphere;
        shadow = CreateShadow(new Vector3(rest.x, groundY + 0.01f, rest.z));
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Color color = shadowMaterial != null ? shadowMaterial.GetColor(baseColor) : Color.black;

        for (float t = 0f; t < fallTime; t += Time.deltaTime)
        {
            float k = t / fallTime;
            transform.position = rest + Vector3.up * (dropHeight * (1f - k * k));
            transform.rotation = Quaternion.AngleAxis(spinSpeed * t, spinAxis) * upright;

            if (shadow != null)
            {
                float size = Mathf.Lerp(2f, 1f, k) * shadowSize;
                shadow.transform.localScale = new Vector3(size, 0.005f, size);
                block.SetColor(baseColor, new Color(color.r, color.g, color.b, color.a * Mathf.Lerp(0.15f, 1f, k)));
                shadow.SetPropertyBlock(block);
            }
            yield return null;
        }

        Quaternion landed = transform.rotation;
        for (float t = 0f; t < bounceTime; t += Time.deltaTime)
        {
            float k = t / bounceTime;
            transform.position = rest + Vector3.up * (bounceHeight * 4f * k * (1f - k));
            transform.rotation = Quaternion.Slerp(landed, upright, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }

        transform.position = rest;
        transform.rotation = upright;
        DestroyShadow();

        foreach (Collider c in colliders)
        {
            if (c != null) c.enabled = true;
        }
    }

    private void OnDestroy()
    {
        DestroyShadow();
    }

    private void DestroyShadow()
    {
        if (shadow != null)
        {
            Destroy(shadow.gameObject);
            shadow = null;
        }
    }

    private Renderer CreateShadow(Vector3 position)
    {
        if (shadowMaterial == null) return null;

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "DropShadow";
        Destroy(disc.GetComponent<Collider>());
        disc.transform.position = position;

        Renderer renderer = disc.GetComponent<Renderer>();
        renderer.sharedMaterial = shadowMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return renderer;
    }
}
