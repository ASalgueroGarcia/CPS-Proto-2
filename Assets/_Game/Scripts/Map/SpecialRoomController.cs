using UnityEngine;

public class SpecialRoomController : MonoBehaviour
{
    [Header("Room Layout")]
    [SerializeField] private Vector3 propPosition = new Vector3(0f, 0.75f, 0f);
    [Min(0.5f)] [SerializeField] private float interactionRadius = 2.6f;
    [SerializeField] private SpecialRoomInteractable interactable;

    public SpecialRoomInteractable Interactable => interactable;

    public void Initialize(SpecialRoomKind kind, RoomRewards? rewards = null)
    {
        if (interactable == null)
        {
            interactable = GetComponentInChildren<SpecialRoomInteractable>(true);
        }

        if (interactable == null)
        {
            interactable = BuildPlaceholderProp(kind);
        }

        interactable.Configure(kind, rewards);
    }

    public static GameObject CreateRuntimeRoom(SpecialRoomKind kind, RoomRewards? rewards = null)
    {
        var root = new GameObject($"SpecialRoom_{kind}");
        var controller = root.AddComponent<SpecialRoomController>();
        controller.Initialize(kind, rewards);
        return root;
    }

    private SpecialRoomInteractable BuildPlaceholderProp(SpecialRoomKind kind)
    {
        var propRoot = new GameObject($"Prop_{kind}");
        propRoot.transform.SetParent(transform, false);
        propRoot.transform.localPosition = propPosition;

        SphereCollider trigger = propRoot.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = interactionRadius;

        PrimitiveType shape = kind switch
        {
            SpecialRoomKind.Rest => PrimitiveType.Cylinder,
            SpecialRoomKind.Health => PrimitiveType.Capsule,
            SpecialRoomKind.Treasure => PrimitiveType.Cube,
            SpecialRoomKind.Deal => PrimitiveType.Sphere,
            SpecialRoomKind.Narrative => PrimitiveType.Capsule,
            _ => PrimitiveType.Cube
        };

        GameObject activeObj = GameObject.CreatePrimitive(shape);
        activeObj.name = "ActiveVisual";
        activeObj.transform.SetParent(propRoot.transform, false);
        activeObj.transform.localPosition = Vector3.zero;
        activeObj.transform.localScale = new Vector3(1.4f, 1.1f, 1.4f);

        Renderer activeRenderer = activeObj.GetComponent<Renderer>();
        if (activeRenderer != null && activeRenderer.sharedMaterial != null)
        {
            var block = new MaterialPropertyBlock();
            Color color = PropColor(kind);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            activeRenderer.SetPropertyBlock(block);
        }

        GameObject spentObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spentObj.name = "SpentVisual";
        spentObj.transform.SetParent(propRoot.transform, false);
        spentObj.transform.localPosition = new Vector3(0f, -0.35f, 0f);
        spentObj.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
        spentObj.SetActive(false);

        Renderer spentRenderer = spentObj.GetComponent<Renderer>();
        if (spentRenderer != null && spentRenderer.sharedMaterial != null)
        {
            var block = new MaterialPropertyBlock();
            Color spent = new Color(0.28f, 0.28f, 0.28f, 1f);
            block.SetColor("_BaseColor", spent);
            block.SetColor("_Color", spent);
            spentRenderer.SetPropertyBlock(block);
        }

        SpecialRoomInteractable comp = propRoot.AddComponent<SpecialRoomInteractable>();
        comp.BindVisuals(activeObj, spentObj, activeRenderer);
        return comp;
    }

    private static Color PropColor(SpecialRoomKind kind)
    {
        switch (kind)
        {
            case SpecialRoomKind.Rest:
            case SpecialRoomKind.Health:
                return new Color(0.25f, 0.78f, 0.42f, 1f);
            case SpecialRoomKind.Treasure:
                return new Color(0.95f, 0.76f, 0.18f, 1f);
            case SpecialRoomKind.Deal:
                return new Color(0.76f, 0.22f, 0.72f, 1f);
            case SpecialRoomKind.Narrative:
                return new Color(0.32f, 0.62f, 0.92f, 1f);
            default:
                return Color.white;
        }
    }
}
