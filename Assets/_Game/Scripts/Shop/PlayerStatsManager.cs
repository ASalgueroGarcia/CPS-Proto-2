using UnityEngine;
using System.Collections.Generic;

// PLAYER MANAGER FOR THE STATS OF THE PLAYER W THE ITEMS TOO.
public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    // REFS.
    private Health healthPlayer;
    private Player playerController;

    // HEALTH.
    private float currentHealth;
    private float maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

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
        playerController = FindFirstObjectByType<Player>();
        if (playerController == null) return;

        healthPlayer = playerController.GetComponent<Health>();
        
        if (healthPlayer != null)
        {
            healthPlayer.OnHealthChanged.RemoveListener(SyncHealth);
        }

        if (!_hasStats)
        {
            // FIRST TIME: Capture defaults from the player prefab instance
            currentSpeed = playerController.speed;
            currentDashSpeed = playerController.dashSpeed;
            currentNormalDamage = playerController.WeaponBaseDamage;
            currentCritChance = playerController.BaseCritChance;

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
            playerController.speed = currentSpeed;
            playerController.dashSpeed = currentDashSpeed;
            playerController.WeaponBaseDamage = currentNormalDamage;
            playerController.BaseCritChance = currentCritChance;

            // HP carries over between rooms (GDD). A stored 0 (dead) is never carried into a room.
            if (healthPlayer != null && currentHealth > 0f)
            {
                healthPlayer.RestoreHealth(currentHealth, maxHealth);
            }
            Debug.Log("PlayerStatsManager: Applied persistent stats to new player.");
        }

        if (healthPlayer != null)
        {
            healthPlayer.OnHealthChanged.AddListener(SyncHealth);
        }
        
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

    public void ApplyPowerUpEffect(PowerUpData pUp)
    {
        if(pUp == null)
        {
            return;
        }
        inventoryItems++; /////
        listOfInventoryItems.Add(pUp);
        
        // NO SWITCH RN-> JUST NOTIFY THE POWERUP CONCRETE CLASSES TO APPLY THEIR EFFECTS..
        PowerUpNotificator.InstanceP.Notify(pUp);
    }

    // INCREASES.
    public void MoreNormalDamage(float v)
    {
        currentNormalDamage += v;
        if (playerController != null)
        {
            playerController.WeaponBaseDamage = currentNormalDamage;
        }
    }

    public void MoreCriticalDamage(float v)
    {
        currentCriticalDamage += v;
    }

    public void MoreSpecialDamage(float v)
    {
        currentSpecialDamage += v;
    }

    public void MoreSpeed(float v)
    {
        currentSpeed += v;
        if (playerController != null)
        {
            playerController.speed = currentSpeed;
        }
    }
    public void MoreDashSpeed(float v)
    {
        currentDashSpeed += v;
        if (playerController != null)
        {
            playerController.dashSpeed = currentDashSpeed;
        }
    }

    public void ApplyStun(float v)
    {
        Debug.Log("STUNNED");
        /////
    }

    public void MoreMaxHealth(float v)
    {
        if (healthPlayer != null)
        {

            healthPlayer.maxHealth += v;
            healthPlayer.currentHealth += v;
        }
    }

    public void MoreEnemyLootRate(float value)
    {
        Debug.Log("MORE LOOT!!");
    }

    public void MoreEnemySpawnRate(float value)
    {
        Debug.Log("MORE ENEMIES!!");
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

        // Let go of the old run's player instead of re-capturing it: after a boss win it is still
        // alive and damaged here, and its HP would leak into the next run. The next room's player
        // is captured fresh when it loads (UIManager.OnSceneLoaded -> BindToPlayer).
        if (healthPlayer != null) healthPlayer.OnHealthChanged.RemoveListener(SyncHealth);
        healthPlayer = null;
        playerController = null;
    }
}