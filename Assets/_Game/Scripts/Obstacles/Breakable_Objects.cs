using UnityEngine;

public class Breakable_Objects : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private int hp = 2;

    [Header("SFX Settings")]
    [SerializeField] private AudioClip hitImpactClip;

    public void TakeDamage(int dmg)
    {
        SoundManager.Instance.PlaySound(hitImpactClip);
        hp -= dmg;
        if (hp <= 0)
        {
            Destroy(gameObject);   
        }
    }

    private void OnCollisionEnter(Collision c)
    {
        if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Enemy")) return;
        
        TakeDamage((int)collision.gameObject.GetComponent<Player>().GetPlayerDamage());
    }
}