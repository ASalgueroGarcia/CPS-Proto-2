using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

// PLAYER MANAGER FOR THE STATS OF THE PLAYER W THE ITEMS TOO.
public class PlayerStatsManager : MonoBehaviour
{
    // REFS.
    private Health healthPlayer;
    private PlayerFSM playerController;
    private WaveManager waveManager;

    // HEALTH.
    private float currentHealth;
    private float maxHealth;

    // CURRENCY.
    private int currentCoins = 0;

    // DAMAGE.
    private float currentNormalDamage;
    private float currentCriticalDamage;
    private float currentSpecialDamage;
    private float currentCritChance;
    private float currentAttackSpeed;

    // SPEED.
    private float currentSpeed;
    private float currentDashSpeed;

    // How many items do u collect? -> INVENTORY.
    private int inventoryItems = 0;
    private List<PowerUpData> listOfInventoryItems = new List<PowerUpData>();

    private void Start()
    {
        playerController = GetComponent<PlayerFSM>();
        healthPlayer = GetComponent<Health>();
        waveManager = FindFirstObjectByType<WaveManager>();

        // INITS.
        if (playerController != null)
        {
            currentSpeed = playerController.speed;
            currentDashSpeed = playerController.dashSpeed;
            currentNormalDamage = playerController.weaponBaseDamage;
            currentCritChance = playerController.baseCritChance;
        }

        if (healthPlayer != null)
        {
            currentHealth = healthPlayer.currentHealth;
            maxHealth = healthPlayer.maxHealth;
        }
        Prints();
    }
    public void AddCoins(int amount)
    {
        currentCoins += amount;
        Debug.Log($"Coins collected: {amount}. Total: {currentCoins}");
    }

    public void Heal(float amount)
    {
        if (healthPlayer != null)
        {
            healthPlayer.currentHealth = Mathf.Min(healthPlayer.currentHealth + amount, healthPlayer.maxHealth);
            currentHealth = healthPlayer.currentHealth;
            Debug.Log($"Healed for {amount}. Current health: {currentHealth}/{maxHealth}");
        }
    }

    public void ApplyPowerUpEffect(PowerUpData powerUp)
    {
        if(powerUp == null)
        {
            return;
        }
        inventoryItems++; /////
        listOfInventoryItems.Add(powerUp);

        // APPLY THE POWERUP DEPENDS OF THE CHOICE OF THE PLAYER.
        for(int i = 0; i < powerUp.effects.Length; i++)
        {
            var effect = powerUp.effects[i];
            switch (effect.type)
            {
                // DAMAGE.
                case PowerUpData.PowerUpType.NormalDamage:
                    currentNormalDamage += effect.value;
                    if (playerController != null){
                        playerController.weaponBaseDamage = currentNormalDamage;
                    }
                    break;

                case PowerUpData.PowerUpType.CriticalDamage:
                    currentCriticalDamage += effect.value;
                    break;

                case PowerUpData.PowerUpType.SpecialDamage:
                    currentSpecialDamage += effect.value;
                    break;

                // SPEED.
                case PowerUpData.PowerUpType.NormalSpeed:
                    currentSpeed += effect.value;
                    if (playerController != null){
                        playerController.speed = currentSpeed;
                    }
                    break;

                case PowerUpData.PowerUpType.DashSpeed:
                    currentDashSpeed += effect.value;
                    if (playerController != null){
                        playerController.dashSpeed = currentDashSpeed;
                    }
                    break;

                case PowerUpData.PowerUpType.Stun:
                    break;

                // DEFENSE & HEALTH
                case PowerUpData.PowerUpType.Health:
                    currentHealth += effect.value;
                    maxHealth += effect.value;
                    if (healthPlayer != null)
                    {
                        healthPlayer.maxHealth = maxHealth;
                        healthPlayer.currentHealth = Mathf.Min(healthPlayer.currentHealth + effect.value, maxHealth);
                    }
                    break;

                // LOOT
                case PowerUpData.PowerUpType.enemyLoot:
                    break;

                case PowerUpData.PowerUpType.enemySpawn:
                    // WaveManager doesn't have spawnInterval yet, maybe increase budget?
                    if (waveManager != null)
                    {
                        // Placeholder for wave modification
                        Debug.Log("Increasing wave difficulty via powerup");
                    }
                    break;
                default:
                    break;
            }
        }
        Prints();
    }
    public void ResetAllThePlayerStats()
    {
        if (playerController != null)
        {
            currentSpeed = playerController.speed;
            currentDashSpeed = playerController.dashSpeed;
            currentNormalDamage = playerController.weaponBaseDamage;
            currentCritChance = playerController.baseCritChance;
        }

        if (healthPlayer != null)
        {
            currentHealth = healthPlayer.currentHealth;
            maxHealth = healthPlayer.maxHealth;
        }

        currentCriticalDamage = 25.0f;
        currentSpecialDamage = 20.0f;
        currentAttackSpeed = 1.0f;
        currentCoins = 0;
        inventoryItems = 0;
        listOfInventoryItems.Clear();
    }

    public void Prints()
    {
        Debug.Log($"Daño Normal: {currentNormalDamage}");
        Debug.Log($"Daño Crítico: {currentCriticalDamage} ({currentCritChance * 100}%)");
        Debug.Log($"Daño Especial: {currentSpecialDamage}");
        Debug.Log($"Velocidad: {currentSpeed}");
        Debug.Log($"Dash Speed: {currentDashSpeed}");
        Debug.Log($"Attack Speed: {currentAttackSpeed}");
        Debug.Log($"Salud: {currentHealth}/{maxHealth}");
        Debug.Log($"Items: {inventoryItems}");
    }
}