using UnityEngine;

public class Coin : MonoBehaviour
{
    public event System.Action OnCollected;
    [SerializeField] private int amount = 1;
    [SerializeField] private AudioClip pickUpClip;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || PlayerStatsManager.Instance == null) return;
        
        SoundManager.Instance.PlaySound(pickUpClip);
        
        PlayerStatsManager.Instance.AddCoins(amount);
        OnCollected?.Invoke();
        Destroy(gameObject);
    }
}
