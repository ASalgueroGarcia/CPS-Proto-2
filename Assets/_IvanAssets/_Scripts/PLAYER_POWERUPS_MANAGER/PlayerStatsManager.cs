using UnityEngine;
using System.Collections.Generic;

// PLAYER MANAGER FOR THE STATS OF THE PLAYER W THE ITEMS TOO.
public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    // REFS.
    private Health healthPlayer;
    private PlayerFSM playerController;
    private WaveManager waveManager;

    // HEALTH.
    private float currentHealth;
    private float maxHealth;

    // CURRENCY.
    private int currentCoins = 0;
    public int CurrentCoins => currentCoins;

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
    public int InventoryItemsCount => inventoryItems;

    private List<PowerUpData> listOfInventoryItems = new List<PowerUpData>();
    public List<PowerUpData> InventoryItems => listOfInventoryItems;

    private bool _hasStats = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // If attached to UI prefab which has DontDestroyOnLoad, this remains persistent.
    }

    private void Start()
    {
        BindToPlayer();
    }

    public void BindToPlayer()
    {
        playerController = FindFirstObjectByType<PlayerFSM>();
        if (playerController == null) return;

        healthPlayer = playerController.GetComponent<Health>();
        
        // Unsubscribe from old health if any
        if (healthPlayer != null)
        {
            healthPlayer.OnHealthChanged.RemoveListener(SyncHealth);
        }

        if (!_hasStats)
        {
            // FIRST TIME: Capture defaults from the player prefab instance
            currentSpeed = playerController.speed;
            currentDashSpeed = playerController.dashSpeed;
            currentNormalDamage = playerController.weaponBaseDamage;
            currentCritChance = playerController.baseCritChance;

            if (healthPlayer != null)
            {
                currentHealth = healthPlayer.currentHealth;
                maxHealth = healthPlayer.maxHealth;
            }
            _hasStats = true;
            Debug.Log("PlayerStatsManager: Captured initial player stats.");
        }
        else
        {
            // SUBSEQUENT TIMES: Apply our persistent stats to the new player instance
            playerController.speed = currentSpeed;
            playerController.dashSpeed = currentDashSpeed;
            playerController.weaponBaseDamage = currentNormalDamage;
            playerController.baseCritChance = currentCritChance;

            if (healthPlayer != null)
            {
                healthPlayer.maxHealth = maxHealth;
                healthPlayer.currentHealth = currentHealth;
            }
            Debug.Log("PlayerStatsManager: Applied persistent stats to new player.");
        }

        if (healthPlayer != null)
        {
            healthPlayer.OnHealthChanged.AddListener(SyncHealth);
        }
        
        waveManager = FindFirstObjectByType<WaveManager>();
        Prints();
    }

    private void OnDestroy()
    {
        if (healthPlayer != null)
        {
            healthPlayer.OnHealthChanged.RemoveListener(SyncHealth);
        }
    }

    private void SyncHealth(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
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
                    if (healthPlayer != null)
                    {
                        healthPlayer.maxHealth += effect.value;
                        healthPlayer.currentHealth += effect.value;
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
        _hasStats = false;
        currentCriticalDamage = 25.0f;
        currentSpecialDamage = 20.0f;
        currentAttackSpeed = 1.0f;
        currentCoins = 0;
        inventoryItems = 0;
        listOfInventoryItems.Clear();
        
        // Try to bind immediately if player exists
        BindToPlayer();
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
