using UnityEngine;

public class HitboxProxy : MonoBehaviour
{
    public HitboxTrigger parent;

    private void OnTriggerEnter(Collider other)
    {
        if (parent != null && parent.isActiveAndEnabled)
        {
            parent.OnHit?.Invoke(other);
        }
    }
}
