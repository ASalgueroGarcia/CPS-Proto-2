using UnityEngine;

/// <summary>
/// Debug visualization for <see cref="ScissorsWeapon"/>. Auto-attached via
/// <see cref="RequireComponent"/> when ScissorsWeapon is added to a GameObject.
/// </summary>
[RequireComponent(typeof(ScissorsWeapon))]
public class ScissorsWeaponGizmos : MonoBehaviour
{
    private ScissorsWeapon _weapon;

    private void Awake() => _weapon = GetComponent<ScissorsWeapon>();

    private void DrawRangeGizmos()
    {
        Vector3 forward = transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + forward, _weapon.AttackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + forward, _weapon.SpecialRange);
    }

    private void OnDrawGizmos()
    {
        if (_weapon == null) return;

        if (_weapon.ShowSpecialGizmo)
        {
            Gizmos.color = new Color(0, 1, 1, 0.4f);
            Transform pt = _weapon.PlayerTransform != null ? _weapon.PlayerTransform : transform;
            Gizmos.DrawSphere(pt.position, _weapon.SpecialRange);
        }

        DrawRangeGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (_weapon == null) return;

        DrawRangeGizmos();

        WeaponHitboxRig rig = _weapon.Rig;
        if (rig != null)
        {
            rig.DrawGizmo(0, new Color(1f, 1f, 1f, 0.4f)); // white = general
            rig.DrawGizmo(1, new Color(0f, 1f, 0f, 0.4f)); // green = combo 1-2
            rig.DrawGizmo(2, new Color(1f, 0f, 0f, 0.4f)); // red   = combo 3 finisher
        }
    }
}
