using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class EnemyPresentationSetup
{
    public const string HealthBarPath = "Assets/_Game/Resources/UI/EnemyHealthBar.prefab";

    [MenuItem("Tools/Enemies/Install Shared Presentation")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Leave Play Mode before installing enemy presentation.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPath) == null)
            CreateHealthBar();

        int migrated = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs/Enemies" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab.GetComponent<Enemy>() == null && prefab.GetComponent<EnemyBase>() == null)
                continue;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = scope.prefabContentsRoot;
                foreach (WorldSpaceHealthBar oldBar in root.GetComponentsInChildren<WorldSpaceHealthBar>(true))
                {
                    if (oldBar.gameObject != root) Object.DestroyImmediate(oldBar.gameObject);
                }

                if (root.GetComponent<EnemyUIAutoSetup>() == null)
                    root.AddComponent<EnemyUIAutoSetup>();
                EnemyVisualFeedback feedback = root.GetComponent<EnemyVisualFeedback>();
                if (feedback == null) feedback = root.AddComponent<EnemyVisualFeedback>();
                Health health = root.GetComponent<Health>();
                var healthSettings = new SerializedObject(health);
                Renderer body = healthSettings.FindProperty("targetRenderer").objectReferenceValue as Renderer;
                if (body == null) body = root.GetComponentInChildren<MeshRenderer>();
                var settings = new SerializedObject(feedback);
                settings.FindProperty("targetRenderer").objectReferenceValue = body;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            migrated++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyPresentation] Shared health bar installed; {migrated} enemy prefabs configured.");
    }

    private static void CreateHealthBar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(HealthBarPath));
        AssetDatabase.Refresh();
        GameObject root = new GameObject("EnemyHealthBar", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(WorldSpaceHealthBar));
        try
        {
            root.transform.localScale = Vector3.one * 0.01f;
            ((RectTransform)root.transform).sizeDelta = new Vector2(160f, 16f);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

            GameObject sliderObject = new GameObject("HealthSlider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(root.transform, false);
            Stretch((RectTransform)sliderObject.transform, Vector2.zero, Vector2.zero);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            Image background = CreateImage("Background", sliderObject.transform, new Color(0.08f, 0.08f, 0.08f, 0.9f));
            Stretch(background.rectTransform, Vector2.zero, Vector2.zero);
            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            Stretch((RectTransform)fillArea.transform, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            Image fill = CreateImage("Fill", fillArea.transform, new Color(0.2f, 0.85f, 0.3f));
            Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;

            Gradient gradient = new Gradient();
            gradient.SetKeys(new[] {
                new GradientColorKey(new Color(0.95f, 0.2f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.15f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.85f, 0.3f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            var settings = new SerializedObject(root.GetComponent<WorldSpaceHealthBar>());
            settings.FindProperty("healthSlider").objectReferenceValue = slider;
            settings.FindProperty("fillImage").objectReferenceValue = fill;
            settings.FindProperty("colorGradient").gradientValue = gradient;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HealthBarPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minimum;
        rect.offsetMax = maximum;
    }
}
