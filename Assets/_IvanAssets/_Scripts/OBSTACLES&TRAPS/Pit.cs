using UnityEngine;

// Block ground-based movement (Player, Enemies) but allow projectiles to pass over them.
public class Pit : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            Health h = other.GetComponent<Health>();
            if(h != null)
            {
                h.TakeDamage(100);//instakill.
            }
        }
    }
}
