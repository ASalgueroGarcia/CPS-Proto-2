using UnityEngine;

public class Brekeable_Objects : MonoBehaviour
{
    [SerializeField] private int hp=2;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy")){
            hp--;

            if (hp <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
