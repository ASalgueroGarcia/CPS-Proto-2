using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The places the camera can be: one camera point each, placed by design in the editor.</summary>
public enum CameraStop
{
    Menu,
    Shop,
    Lobby,
    Arena,
}

/// <summary>
/// The wagon's camera: four camera points (Menu, Shop, Lobby, Arena) that design places in the
/// editor, and a smooth move between them. Built on Iván's CameraSystem / CameraRotationSystem
/// (Shop/LOBBY_SHOP, PR #79): a list of points, each with a position and a facing, and a
/// SmoothStep move. On top of that: the camera never rolls, the move runs on unscaled time (a paused
/// game can't stall it), and the parts Sarah split off the wagon switch off while they block the shot.
///
/// To place a point, select it under CameraPoints and move/rotate it: the camera looks along the
/// point's blue (forward) axis. Each point draws a small camera in the Scene view.
/// As the hub-entry flight (ICameraFlight) it flies to the Lobby point.
/// </summary>
public class CameraStops : MonoBehaviour, ICameraFlight
{
    [Header("CAMERA")]
    [Tooltip("The camera to move. Empty = Camera.main.")]
    [SerializeField] private Transform flyingCamera;
    [Tooltip("Put the camera on the Menu point when the scene starts.")]
    [SerializeField] private bool startAtMenu = true;

    [Header("CAMERA POINTS (move / rotate these in the editor)")]
    [SerializeField] private Transform menu;
    [SerializeField] private Transform shop;
    [SerializeField] private Transform lobby;
    [SerializeField] private Transform arena;

    [Header("MOVE")]
    [SerializeField, Min(0.01f)] private float duration = 3f;

    [Header("PARTS THAT GET OUT OF THE WAY")]
    [SerializeField] private Renderer[] hideWhenBlocking = Array.Empty<Renderer>();
    [Tooltip("Extra margin (world units) around each part's bounds.")]
    [SerializeField, Min(0f)] private float hidePadding = 0.1f;
    [Tooltip("How far ahead of the camera a part still counts as blocking the view.")]
    [SerializeField, Min(0.1f)] private float viewDepth = 3f;

    private readonly List<Renderer> _hidden = new List<Renderer>();
    private Bounds[] _partBounds = Array.Empty<Bounds>();
    private Coroutine _running;

    public CameraStop Current { get; private set; } = CameraStop.Menu;
    public bool IsMoving => _running != null;

    private Transform FlyingCamera => flyingCamera != null ? flyingCamera : Camera.main != null ? Camera.main.transform : null;

    private void Start()
    {
        if (startAtMenu) SnapTo(CameraStop.Menu);
    }

    /// <summary>The camera point for a stop (null if design hasn't set it).</summary>
    public Transform PointFor(CameraStop stop)
    {
        switch (stop)
        {
            case CameraStop.Menu: return menu;
            case CameraStop.Shop: return shop;
            case CameraStop.Lobby: return lobby;
            default: return arena;
        }
    }

    /// <summary>Puts the camera on a point at once.</summary>
    public void SnapTo(CameraStop stop)
    {
        Transform cam = FlyingCamera, point = PointFor(stop);
        if (cam == null || point == null) return;

        if (_running != null) StopCoroutine(_running);
        _running = null;
        cam.SetPositionAndRotation(point.position, Level(point.rotation));
        Current = stop;
    }

    /// <summary>Moves the camera to a point; <paramref name="onArrived"/> runs once it's there.</summary>
    public void FlyTo(CameraStop stop, Action onArrived = null)
    {
        Transform cam = FlyingCamera, point = PointFor(stop);
        if (cam == null || point == null)
        {
            Debug.LogWarning($"[CameraStops] No camera or no {stop} point - skipping the move.", this);
            onArrived?.Invoke();
            return;
        }

        // The set doesn't move, so measure the parts once, while they're all still visible.
        _partBounds = new Bounds[hideWhenBlocking.Length];
        for (int i = 0; i < hideWhenBlocking.Length; i++)
        {
            if (hideWhenBlocking[i] != null) _partBounds[i] = hideWhenBlocking[i].bounds;
        }

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Fly(cam, point, stop, onArrived));
    }

    // ICameraFlight: picking a save slot flies the camera into the lobby.
    public void Play(Action onFinished) => FlyTo(CameraStop.Lobby, onFinished);

    private IEnumerator Fly(Transform cam, Transform point, CameraStop stop, Action onArrived)
    {
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;
        Vector3 endPos = point.position;
        Quaternion endRot = Level(point.rotation);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cam.SetPositionAndRotation(Vector3.Lerp(startPos, endPos, t), Turn(startRot, endRot, t));
            UpdateHiddenParts(cam);
            yield return null;
        }

        cam.SetPositionAndRotation(endPos, endRot);
        UpdateHiddenParts(cam);
        Current = stop;
        _running = null;
        onArrived?.Invoke();
    }

    private void UpdateHiddenParts(Transform cam)
    {
        Vector3 focus = cam.position + cam.forward * viewDepth;
        for (int i = 0; i < hideWhenBlocking.Length; i++)
        {
            Renderer part = hideWhenBlocking[i];
            if (part == null) continue;

            bool blocks = BlocksView(_partBounds[i], cam.position, focus, hidePadding);
            if (blocks && part.enabled)
            {
                part.enabled = false;
                _hidden.Add(part);
            }
            else if (!blocks && !part.enabled && _hidden.Remove(part))
            {
                part.enabled = true;
            }
        }
    }

    private void OnDisable()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        foreach (Renderer part in _hidden)
        {
            if (part != null) part.enabled = true;
        }

        _hidden.Clear();
    }

    /// <summary>True when the camera is inside the part or the part sits between the camera and its focus.</summary>
    public static bool BlocksView(Bounds part, Vector3 camera, Vector3 focus, float padding)
    {
        part.Expand(padding * 2f);
        if (part.Contains(camera)) return true;

        Vector3 toFocus = focus - camera;
        float distance = toFocus.magnitude;
        if (distance < 0.0001f) return false;

        return part.IntersectRay(new Ray(camera, toFocus / distance), out float hit) && hit <= distance;
    }

    /// <summary>
    /// Turns from one facing to another at <paramref name="t"/> (0..1), kept level: blending two
    /// upright views with different pitch and heading would otherwise tilt the horizon mid-turn.
    /// </summary>
    public static Quaternion Turn(Quaternion from, Quaternion to, float t) => Level(Quaternion.Slerp(from, to, t));

    /// <summary>The same facing with the roll removed (the horizon stays flat).</summary>
    public static Quaternion Level(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        return Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.999f ? rotation : Quaternion.LookRotation(forward, Vector3.up);
    }

    private void OnDrawGizmos()
    {
        DrawPoint(menu, new Color(1f, 0.85f, 0.2f));
        DrawPoint(shop, new Color(0.3f, 0.9f, 0.4f));
        DrawPoint(lobby, new Color(1f, 0.5f, 0.2f));
        DrawPoint(arena, new Color(0.9f, 0.3f, 0.3f));
    }

    private static void DrawPoint(Transform point, Color color)
    {
        if (point == null) return;

        Gizmos.color = color;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(point.position, Level(point.rotation), Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, 60f, 1.2f, 0.05f, 16f / 9f);
        Gizmos.matrix = previous;
        Gizmos.DrawSphere(point.position, 0.08f);
    }
}
