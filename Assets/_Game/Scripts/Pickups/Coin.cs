using System;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public event Action OnCollected;

    [SerializeField] private int amount = 1;
    [SerializeField] private AudioClip pickUpClip;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || PlayerStatsManager.Instance == null) return;

        if (SoundManager.Instance != null && pickUpClip != null)
        {
            SoundManager.Instance.PlaySound(pickUpClip);
        }

        PlayerStatsManager.Instance.AddCoins(amount);
        OnCollected?.Invoke();
        Destroy(gameObject);
    }
}
