using System;
using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    public Action<Collider> OnHit;

    private void OnTriggerEnter(Collider other)
    {
        OnHit?.Invoke(other);
    }

    private void OnDestroy()
    {
        OnHit = null;
    }
}
