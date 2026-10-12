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
        if (!c.gameObject.CompareTag("Player") && !c.gameObject.CompareTag("Enemy")) return;
        
        if (c.gameObject.TryGetComponent(out Player player))
            TakeDamage((int)player.GetPlayerDamage());
    }
}