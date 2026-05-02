using UnityEngine;

public class Brekeable_Objects : MonoBehaviour
{
    [SerializeField] private int hp = 2;

    public void TakeDamage(int amount)
    {
        hp -= amount;
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy"))
        {
            hp--;

            if (hp <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
