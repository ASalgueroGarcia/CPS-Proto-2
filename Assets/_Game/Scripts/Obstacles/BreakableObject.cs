using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private int hp = 2;

    [Header("SFX Settings")]
    [SerializeField] private AudioClip hitImpactClip;

    public void TakeDamage(int amount)
    {
        SoundManager.Instance.PlaySound(hitImpactClip);
        
        hp -= amount;
        if (hp <= 0) Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Enemy")) return;

        var player = collision.gameObject.GetComponent<PlayerFSM>();
        TakeDamage(player != null ? (int)player.GetPlayerDamage() : 1);
    }
}
