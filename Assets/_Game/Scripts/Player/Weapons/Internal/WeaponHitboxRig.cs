using System;
using UnityEngine;
using Core.Utils;

/// <summary>
/// Generic hitbox rig. Manages the lifecycle of N hitbox GameObjects for a weapon, 
/// including enabling/disabling, subscribing to hit events, and drawing gizmos for debugging.
/// </summary>
public class WeaponHitboxRig
{

#region Fields and Properties

    private readonly GameObject[] _hitboxes;
    private readonly HitboxTrigger[] _triggers;
    private readonly Action<Collider> _onHit;

    public bool HasActiveHitbox { get; private set; }

#endregion

    public WeaponHitboxRig(GameObject[] hitboxes, Action<Collider> onHit)
    {
        _hitboxes = hitboxes ?? Array.Empty<GameObject>();
        _triggers = new HitboxTrigger[_hitboxes.Length];
        _onHit = onHit;

        for (int i = 0; i < _hitboxes.Length; i++)
            _triggers[i] = SetupHitbox(_hitboxes[i]);
    }

#region Public Methods

    /// <summary>
    /// Enables the hitbox at the given index (and disables all others).
    /// </summary>
    public void Enable(int index)
    {
        DisableAll();
        if (index < 0 || index >= _hitboxes.Length) return;

        var go = _hitboxes[index];
        if (go == null) return;

        go.SetActive(true);
        HasActiveHitbox = true;
    }

    /// <summary>
    /// Enables the first non-null hitbox found, skipping nulls.
    /// </summary>
    public void EnableFirstAvailable()
    {
        for (int i = 0; i < _hitboxes.Length; i++)
        {
            if (_hitboxes[i] != null)
            {
                Enable(i);
                return;
            }
        }
    }

    public void DisableAll()
    {
        for (int i = 0; i < _hitboxes.Length; i++)
        {
            if (_hitboxes[i] != null)
                _hitboxes[i].SetActive(false);
        }
        HasActiveHitbox = false;
    }

    public void SubscribeAll()
    {
        for (int i = 0; i < _triggers.Length; i++)
        {
            if (_triggers[i] != null && _onHit != null)
                _triggers[i].OnHit += _onHit;
        }
    }

    public void UnsubscribeAll()
    {
        for (int i = 0; i < _triggers.Length; i++)
        {
            if (_triggers[i] != null && _onHit != null)
                _triggers[i].OnHit -= _onHit;
        }
    }

    /// <summary>
    /// Returns true if the given index is valid for this rig, false otherwise. Valid indices are 0 to Count-1.
    /// </summary>
    public bool IsValidIndex(int index) => index >= 0 && index < _hitboxes.Length;

    /// <summary>
    /// Returns the hitbox GameObject at the given index, or null if the index is invalid.
    /// </summary>
    public GameObject GetHitbox(int index) => IsValidIndex(index) ? _hitboxes[index] : null;

    /// <summary>
    /// Returns the number of hitboxes in this rig. This is the same as the length of the hitbox array.
    /// </summary>
    public int Count => _hitboxes.Length;

    /// <summary>
    /// Draws the actual collider shape of every hitbox in the rig using Gizmos.
    /// Call from OnDrawGizmos / OnDrawGizmosSelected on the owning MonoBehaviour.
    /// </summary>
    public void DrawGizmos(Color color)
    {
        for (int i = 0; i < _hitboxes.Length; i++)
            DrawHitboxGizmo(_hitboxes[i], color);
    }

    /// <summary>
    /// Draws the collider of the hitbox at the given index using Gizmos.
    /// </summary>
    public void DrawGizmo(int index, Color color)
    {
        if (!IsValidIndex(index)) return;

        DrawHitboxGizmo(_hitboxes[index], color);
    }

#endregion
#region Private Methods

    /// <summary>
    /// Draws the collider of the given GameObject using Gizmos. Supports BoxCollider,
    /// SphereCollider, CapsuleCollider, and MeshCollider. Ignores null GameObjects and colliders.
    /// </summary>
    private static void DrawHitboxGizmo(GameObject go, Color color)
    {
        if (go == null) return;

        Collider[] colliders = go.GetComponentsInChildren<Collider>(true);
        Gizmos.color = color;

        foreach (Collider col in colliders)
        {
            if (col == null) continue;

            Gizmos.matrix = col.transform.localToWorldMatrix;

            switch (col)
            {
                case BoxCollider box:
                    Gizmos.DrawCube(box.center, box.size);
                    break;
                case SphereCollider sphere:
                    Gizmos.DrawSphere(sphere.center, sphere.radius * 2f);
                    break;
                case CapsuleCollider capsule:
                    // Capsule is approximated as a sphere at its center
                    Gizmos.DrawSphere(capsule.center, capsule.radius * 2f);
                    break;
                case MeshCollider mesh when mesh.sharedMesh != null:
                    Gizmos.DrawMesh(mesh.sharedMesh);
                    break;
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    /// <summary>
    /// Sets up a hitbox for the given GameObject.
    /// </summary>
    private static HitboxTrigger SetupHitbox(GameObject go)
    {
        if (go == null) return null;

        // Ensure the GameObject has a HitboxTrigger component
        go.GetOrAddComponent<HitboxTrigger>();

        Collider col = go.GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;

        Rigidbody rb = go.GetOrAddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;

        HitboxTrigger parentTrigger = go.GetComponent<HitboxTrigger>();

        foreach (Collider childCol in go.GetComponentsInChildren<Collider>(true))
        {
            childCol.isTrigger = true;

            // Ensure each child collider has a HitboxProxy component
            HitboxProxy proxy = childCol.gameObject.GetOrAddComponent<HitboxProxy>();

            proxy.parent = parentTrigger;
        }

        return parentTrigger;
    }

#endregion
}
