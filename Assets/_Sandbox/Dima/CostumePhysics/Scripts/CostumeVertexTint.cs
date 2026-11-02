using UnityEngine;

// Sandbox only: Sarah's mesh has no materials yet, so a plain grey body hides the robe's motion. At start this
// colours a copy of the mesh by bone weight (costume bones vs the rest) for a vertex-colour material.
[RequireComponent(typeof(SkinnedMeshRenderer))]
public class CostumeVertexTint : MonoBehaviour
{
    [SerializeField] private Color bodyColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color costumeColor = new Color(0.25f, 0.45f, 0.9f);
    [Tooltip("Bones whose name starts with one of these count as costume.")]
    [SerializeField] private string[] costumeBonePrefixes = { "Cloth", "Skirt", "Belt", "HeadCloth" };

    private Mesh tinted;

    private void Awake()
    {
        var skinned = GetComponent<SkinnedMeshRenderer>();
        if (!skinned.sharedMesh.isReadable)
        {
            Debug.LogWarning($"CostumeVertexTint: {skinned.sharedMesh.name} needs Read/Write enabled in its import settings.", this);
            return;
        }

        var bones = skinned.bones;
        var isCostume = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++) isCostume[i] = bones[i] != null && IsCostume(bones[i].name);

        tinted = Instantiate(skinned.sharedMesh);
        tinted.name = skinned.sharedMesh.name + " (tinted)";
        var weights = tinted.boneWeights;
        var colors = new Color[weights.Length];
        for (int v = 0; v < weights.Length; v++)
        {
            var w = weights[v];
            float costume = (isCostume[w.boneIndex0] ? w.weight0 : 0f) + (isCostume[w.boneIndex1] ? w.weight1 : 0f)
                          + (isCostume[w.boneIndex2] ? w.weight2 : 0f) + (isCostume[w.boneIndex3] ? w.weight3 : 0f);
            colors[v] = Color.Lerp(bodyColor, costumeColor, costume);
        }

        tinted.colors = colors;
        skinned.sharedMesh = tinted;
    }

    private void OnDestroy()
    {
        if (tinted != null) Destroy(tinted);
    }

    private bool IsCostume(string boneName)
    {
        foreach (var prefix in costumeBonePrefixes)
        {
            if (boneName.StartsWith(prefix)) return true;
        }

        return false;
    }
}
