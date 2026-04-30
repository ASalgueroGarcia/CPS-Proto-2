using System.Collections.Generic;
using UnityEngine;

public class Scissors : MonoBehaviour
{
    private float damage;
    private float critChance;

    // List to keep track of enemies hit during the current rotation
    private List<Health> hitEnemies = new List<Health>();
    private List<Brekeable_Objects> hitBreakables = new List<Brekeable_Objects>();

    public void Initialize(float dmg, float crit)
    {
        damage = dmg;
        critChance = crit;
        hitEnemies.Clear();
        hitBreakables.Clear();

        // Ensure all children with colliders have a proxy script to report back to this main script
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.isTrigger = true;
            ScissorProxy proxy = col.gameObject.GetComponent<ScissorProxy>();
            if (proxy == null) proxy = col.gameObject.AddComponent<ScissorProxy>();
            proxy.parentScissor = this;
        }
    }

    public void ProcessHit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Health enemyHealth = other.GetComponent<Health>();

            if (enemyHealth != null && !hitEnemies.Contains(enemyHealth))
            {
                bool isCrit = Random.value < critChance;
                float finalDamage = isCrit ? damage * 2 : damage;
                
                // Damage + Knockback
                enemyHealth.TakeDamage(finalDamage, transform.root.position, 5f);
                hitEnemies.Add(enemyHealth);
                
                Debug.Log($"Scissor hit {other.name} via {gameObject.name} child for {finalDamage} damage");
            }
        }
        else if (other.CompareTag("Breakeable"))
        {
            Brekeable_Objects breakable = other.GetComponent<Brekeable_Objects>();
            if (breakable != null && !hitBreakables.Contains(breakable))
            {
                breakable.TakeDamage(1);
                hitBreakables.Add(breakable);
                Debug.Log($"Scissor hit breakable {other.name}");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessHit(other);
    }

    private void OnDisable()
    {
        hitEnemies.Clear();
        hitBreakables.Clear();
    }
}

// Small helper class to forward trigger events from children to the parent Scissor script
public class ScissorProxy : MonoBehaviour
{
    public Scissors parentScissor;

    private void OnTriggerEnter(Collider other)
    {
        if (parentScissor != null && parentScissor.isActiveAndEnabled)
        {
            parentScissor.ProcessHit(other);
        }
    }
}