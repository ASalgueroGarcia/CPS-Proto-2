using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyUIAutoSetup : MonoBehaviour
{
    public const string HealthBarResourcePath = "UI/EnemyHealthBar";
    [Min(0f)] [SerializeField] private float heightPadding = 0.35f;
    private GameObject healthBarInstance;

    private void Start()
    {
        GameObject prefab = Resources.Load<GameObject>(HealthBarResourcePath);
        if (prefab == null || prefab.GetComponent<WorldSpaceHealthBar>() == null)
        {
            Debug.LogError("Shared enemy health-bar prefab is missing. Run Tools > Enemies > Install Shared Presentation.", this);
            return;
        }
        foreach (WorldSpaceHealthBar oldBar in GetComponentsInChildren<WorldSpaceHealthBar>(true))
        {
            oldBar.gameObject.SetActive(false);
            Destroy(oldBar.gameObject);
        }

        EnemyVisualFeedback feedback = GetComponent<EnemyVisualFeedback>();
        Renderer body = feedback != null ? feedback.TargetRenderer : GetComponentInChildren<MeshRenderer>();
        healthBarInstance = Instantiate(prefab, transform.position + Vector3.up * (2f + heightPadding), Quaternion.identity);
        WorldSpaceHealthBar bar = healthBarInstance.GetComponent<WorldSpaceHealthBar>();
        bar.Initialize(GetComponent<Health>());
        bar.SetBodyAnchor(body, heightPadding);
    }

    private void OnDestroy()
    {
        if (healthBarInstance != null) Destroy(healthBarInstance);
    }
}
