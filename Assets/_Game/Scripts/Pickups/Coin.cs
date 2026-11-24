using System;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public event Action OnCollected;

    [SerializeField] private int amount = 1;
    [SerializeField] private AudioClip pickUpClip;

    private Transform flyTarget;
    private float flySpeed;
    private bool collected;

    /// <summary>Homes in on <paramref name="target"/> and collects itself on arrival (room-clear Buttons).</summary>
    public void FlyTo(Transform target, float speed)
    {
        // A coin still dropping would fight the flight until its drop ends.
        if (TryGetComponent(out PickupDropAnimation drop)) drop.Land();

        flyTarget = target;
        flySpeed = speed;
    }

    private void Update()
    {
        if (flyTarget == null) return;

        Vector3 goal = flyTarget.position + Vector3.up * 0.5f;
        transform.position = Vector3.MoveTowards(transform.position, goal, flySpeed * Time.deltaTime);
        if ((transform.position - goal).sqrMagnitude < 0.04f) Collect();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    private void Collect()
    {
        if (collected || PlayerStatsManager.Instance == null) return;
        collected = true;

        if (SoundManager.Instance != null && pickUpClip != null)
        {
            SoundManager.Instance.PlaySound(pickUpClip);
        }

        PlayerStatsManager.Instance.AddCoins(amount);
        OnCollected?.Invoke();
        Destroy(gameObject);
    }
}
