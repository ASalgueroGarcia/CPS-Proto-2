using UnityEngine;

public class CombatDummy : MonoBehaviour
{
    // Layer-based identification is handled by the GameObject's Layer setting in Unity.

    [Header("Settings")]
    public bool counterAttack = false;
    public float counterAttackDelay = 0.5f;

    [Header("Visuals")]
    public MeshRenderer dummyRenderer;
    private Color originalColor;

    private void Start()
    {
        if (dummyRenderer != null) originalColor = dummyRenderer.material.color;
    }

    public void TakeDamage(float damage, float critChance)
    {
        bool isCrit = Random.value < critChance;
        float finalDamage = isCrit ? damage * 2 : damage;

        string critText = isCrit ? " <color=red>CRITICAL!</color>" : "";
        Debug.Log($"Dummy took {finalDamage} damage.{critText}");

        FlashColor(Color.red);

        if (counterAttack)
        {
            Invoke("HitPlayerBack", counterAttackDelay);
        }
    }

    private void HitPlayerBack()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 3.0f);
        foreach (var hit in hits)
        {
            PlayerFSM player = hit.GetComponent<PlayerFSM>();
            if (player != null)
            {
                player.OnPlayerHit();
                Debug.Log("Dummy counter-attacked!");
                break;
            }
        }
    }

    private void FlashColor(Color color)
    {
        if (dummyRenderer != null)
        {
            dummyRenderer.material.color = color;
            Invoke("ResetColor", 0.1f);
        }
    }

    private void ResetColor()
    {
        if (dummyRenderer != null) dummyRenderer.material.color = originalColor;
    }
}