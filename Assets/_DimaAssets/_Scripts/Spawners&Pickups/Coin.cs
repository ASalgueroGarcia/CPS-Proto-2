using UnityEngine;

public class Coin : MonoBehaviour
{
    public event System.Action OnCollected;
    [SerializeField] private int amount = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PlayerStatsManager.Instance != null)
            {
                PlayerStatsManager.Instance.AddCoins(amount);
                OnCollected?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
