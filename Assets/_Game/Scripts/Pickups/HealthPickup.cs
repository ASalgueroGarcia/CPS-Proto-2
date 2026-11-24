using System;
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public event Action OnCollected;

    [SerializeField] private float healAmount = 20f;

    public float HealAmount
    {
        get => healAmount;
        set => healAmount = value;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.Heal(healAmount);
            OnCollected?.Invoke();
            Destroy(gameObject);
        }
    }
}
