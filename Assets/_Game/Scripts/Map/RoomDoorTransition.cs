using System.Collections;
using Core.Utils;
using UnityEngine;

// NOTE / PLACEHOLDER: The door transition and glowing floor exit button are temporary placeholders.
// They might not work consistently right now because the room maps do not yet follow the full Notion design structure.
// Once the maps and room layouts are updated according to the Notion design, this feature will be fully implemented and refined.
public class RoomDoorTransition : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Vector3 doorSize = new Vector3(3f, 3f, 0.3f);
    [SerializeField] private Material doorMaterial;
    [SerializeField] private float doorMoveTime = 0.6f;

    [Header("Walk")]
    [SerializeField] private float walkDistance = 1f;

    // NOTE / PLACEHOLDER: The glowing floor exit button (mark) is a temporary placeholder.
    // It might not work consistently right now because room maps do not yet follow the full Notion design structure.
    // Once the room layouts and dimensions are finalized according to Notion, this feature will be fully implemented and refined.
    [Header("Exit Mark / Floor Button")]
    [SerializeField] private float markRadius = 0.6f;
    [SerializeField] private Color markColor = new Color(1f, 0.78f, 0.22f);
    [SerializeField] private Material markMaterial;

    [Header("Fade")]
    [SerializeField] private float fadeInTime = 0.6f;
    [SerializeField] private float fadeOutTime = 0.6f;

    private WaveManager waveManager;
    private Player player;
    private Transform door;
    private GameObject mark; // Floor glowing exit button
    private bool exitOpen;
    private bool leaving;

    public void Bind(WaveManager targetWaveManager)
    {
        waveManager = targetWaveManager;
        if (waveManager == null) return;

        waveManager.HoldUntilBegin();
        // Keeps the room's no-spawn zone on this door, wherever the door or the room sits.
        waveManager.SetDoorPosition(transform.position);
    }

    private void OnEnable()
    {
        WaveManager.OnRoomCompleted += OpenExit;
    }

    private void OnDisable()
    {
        WaveManager.OnRoomCompleted -= OpenExit;
    }

    private IEnumerator Start()
    {
        Rigidbody body = gameObject.GetOrAddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        BuildDoor();
        BuildMark();
        player = FindFirstObjectByType<Player>();

        ScreenFader.Instance.Set(1f);
        if (player != null)
        {
            Lock(true);
            Teleport(transform.position + transform.forward * StartInset());
            player.Locomotion.UpdateRotation(transform.forward);
        }

        ScreenFader.Instance.FadeTo(0f, fadeInTime);
        if (player != null) yield return Walk(transform.forward, walkDistance - StartInset());
        yield return MoveDoor(false);
        if (player != null) Lock(false);

        if (waveManager != null) waveManager.Begin();
        else OpenExit();
    }

    private void OpenExit()
    {
        if (exitOpen || this == null) return;
        exitOpen = true;
        StartCoroutine(RaiseDoorAndMark());
    }

    private IEnumerator RaiseDoorAndMark()
    {
        yield return MoveDoor(true);
        mark.SetActive(true);
    }

    // NOTE / PLACEHOLDER: Stepping onto the glowing floor exit button triggers this transition.
    // This is a placeholder until room layouts match Notion specifications.
    private void OnTriggerStay(Collider other)
    {
        if (!mark.activeSelf || leaving || other.GetComponentInParent<Player>() == null) return;
        StartCoroutine(Leave());
    }

    private IEnumerator Leave()
    {
        leaving = true;
        mark.SetActive(false);

        if (player != null) Lock(true);
        Coroutine fade = ScreenFader.Instance.FadeTo(1f, fadeOutTime);
        if (player != null) yield return Walk(-transform.forward, walkDistance - StartInset());
        yield return fade;

        MapBehaviour map = FindFirstObjectByType<MapBehaviour>();
        bool nextRoom = map != null && map.HasNextRoom;
        if (map != null) map.LeaveRoom();
        else SceneController.Instance?.UnloadLevel();

        if (!nextRoom && ScreenFader.Instance != null) ScreenFader.Instance.FadeTo(0f, fadeInTime);
    }

    private IEnumerator Walk(Vector3 direction, float distance)
    {
        if (player == null) yield break;
        Vector3 start = player.transform.position;
        float timeout = distance / Mathf.Max(player.speed, 0.1f) * 2f + 0.5f;

        if (player.Locomotion != null) player.Locomotion.UpdateFromInput(new Vector2(direction.x, direction.z));
        for (float t = 0f; t < timeout && player != null && FlatDistance(start, player.transform.position) < distance; t += Time.deltaTime)
        {
            yield return null;
        }
        if (player != null && player.Locomotion != null) player.Locomotion.UpdateFromInput(Vector2.zero);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    private IEnumerator MoveDoor(bool raised)
    {
        Vector3 from = door.localPosition;
        Vector3 to = DoorPosition(raised);
        for (float t = 0f; t < doorMoveTime; t += Time.deltaTime)
        {
            door.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / doorMoveTime));
            yield return null;
        }
        door.localPosition = to;
    }

    private Vector3 DoorPosition(bool raised)
    {
        return new Vector3(0f, raised ? doorSize.y * 0.5f : -doorSize.y * 0.5f - 0.05f, -doorSize.z * 0.5f);
    }

    private void Lock(bool locked)
    {
        if (locked)
        {
            if (player.currentState == Player.PlayerState.Attacking || player.currentState == Player.PlayerState.SpecialAttacking)
            {
                player.Weapon?.ResetCombo();
            }
            player.SwitchState(Player.PlayerState.Idle);
            player.Locomotion.ResetToIdle();
        }

        player.InputLocked = locked;
    }

    private float StartInset()
    {
        return (player.controller != null ? player.controller.radius : 0.5f) + 0.05f;
    }

    private void Teleport(Vector3 position)
    {
        CharacterController controller = player.controller;
        if (controller != null) controller.enabled = false;
        player.transform.position = new Vector3(position.x, player.transform.position.y, position.z);
        if (controller != null) controller.enabled = true;
    }

    private void BuildDoor()
    {
        GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObj.name = "Door";
        Destroy(doorObj.GetComponent<Collider>());
        door = doorObj.transform;
        door.SetParent(transform, false);
        door.localScale = doorSize;
        door.localPosition = DoorPosition(true);

        Renderer renderer = doorObj.GetComponent<Renderer>();
        if (doorMaterial != null) renderer.sharedMaterial = doorMaterial;
        else renderer.material.color = Color.black;
    }

    // NOTE / PLACEHOLDER: This builds the glowing floor button (exit mark cylinder + point light + trigger).
    // This is a placeholder and might not work consistently right now because the room maps do not yet follow the full Notion design structure.
    // Once the room layouts and dimensions are finalized according to Notion, this feature will be fully implemented and refined.
    private void BuildMark()
    {
        mark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mark.name = "ExitMark";
        Destroy(mark.GetComponent<Collider>());
        mark.transform.SetParent(transform, false);
        mark.transform.localPosition = Vector3.forward * walkDistance + Vector3.up * 0.02f;
        mark.transform.localScale = new Vector3(markRadius * 2f, 0.01f, markRadius * 2f);

        Renderer renderer = mark.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (markMaterial != null) renderer.sharedMaterial = markMaterial;
        else
        {
            Material material = renderer.material;
            material.color = markColor;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", markColor * 2f);
        }

        BoxCollider zone = new GameObject("Trigger").AddComponent<BoxCollider>();
        zone.transform.SetParent(transform, false);
        zone.transform.localPosition = mark.transform.localPosition + Vector3.up;
        zone.size = new Vector3(markRadius * 2f, 2f, markRadius * 2f);
        zone.isTrigger = true;

        Light glow = new GameObject("Glow").AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = markColor;
        glow.range = markRadius * 4f;
        glow.intensity = 2f;
        glow.transform.SetParent(mark.transform, false);
        glow.transform.position = mark.transform.position + Vector3.up * 0.5f;

        mark.SetActive(false);
    }
}
