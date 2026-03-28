using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public event System.Action OnCollected;
    [SerializeField] private float healAmount = 20f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatsManager stats = other.GetComponent<PlayerStatsManager>();
            if (stats != null)
            {
                stats.Heal(healAmount);
                OnCollected?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
