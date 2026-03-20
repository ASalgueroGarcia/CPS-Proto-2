using UnityEngine;

public class CombatDummy : MonoBehaviour
{
    [Header("Settings")]
    public Health dummyHealth; // Link Health script here
    public bool counterAttack = false;
    public float counterAttackDelay = 0.5f;
    public float counterDamage = 15f;

    [Header("Visuals")]
    public MeshRenderer dummyRenderer;
    private Color originalColor;
    private Camera mainCam;

    private void OnEnable()
    {
        if (dummyHealth == null) dummyHealth = GetComponent<Health>();

        if (dummyRenderer != null) originalColor = dummyRenderer.material.color;
        mainCam = Camera.main;
        
        if (dummyHealth != null)
        {
            dummyHealth.OnDamageTaken.AddListener(OnDamage);
        }
    }

    private void OnDisable()
    {
        if (dummyHealth != null)
        {
            dummyHealth.OnDamageTaken.RemoveListener(OnDamage);
        }
    }

    private void OnDamage(float damage)
    {
        FlashColor(Color.red);

        if (counterAttack)
        {
            Invoke("HitPlayerBack", counterAttackDelay);
        }
    }

    private void HitPlayerBack()
    {
        // Search for player specifically by Health component
        Collider[] hits = Physics.OverlapSphere(transform.position, 5.0f);
        foreach (var hit in hits)
        {
            Health playerHealth = hit.GetComponent<Health>();
            // Ensure it's the player (by tag or by having PlayerFSM)
            if (playerHealth != null && hit.GetComponent<PlayerFSM>() != null)
            {
                playerHealth.TakeDamage(counterDamage);
                Debug.Log("Dummy counter-attacked Player!");
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

    private void OnGUI()
    {
        if (dummyHealth == null) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        
        if (screenPos.z > 0) 
        {
            float width = 100f;
            float height = 12f;
            float x = screenPos.x - width / 2;
            float y = Screen.height - screenPos.y - height;

            GUI.color = Color.black;
            GUI.Box(new Rect(x, y, width, height), "");
            GUI.color = Color.red;
            GUI.Box(new Rect(x, y, width * (dummyHealth.currentHealth / dummyHealth.maxHealth), height), "");
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y - 20, width, 20), "DUMMY HP: " + (int)dummyHealth.currentHealth);
        }
    }
}