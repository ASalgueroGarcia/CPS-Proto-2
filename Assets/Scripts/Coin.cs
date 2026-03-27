using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int amount = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStatsManager stats = other.GetComponent<PlayerStatsManager>();
            if (stats != null)
            {
                stats.AddCoins(amount);
                Destroy(gameObject);
            }
        }
    }
}
