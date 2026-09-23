using UnityEngine;
using System.Collections.Generic;

// PLAYER MANAGER FOR THE STATS OF THE PLAYER W THE ITEMS TOO.
public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    // REFS.
    private Health healthPlayer;
    private PlayerFSM playerController;

    // HEALTH.
    private float currentHealth;
    private float maxHealth;

    // CURRENCY.
    private int currentCoins = 0;
    public int CurrentCoins => currentCoins;

    // DAMAGE.
    private float currentNormalDamage;
    private float currentCritChance;

    // SPEED.
    private float currentSpeed;
    private float currentDashSpeed;

    // How many items do u collect? -> INVENTORY.
    private int inventoryItems = 0;
    public int InventoryItemsCount => inventoryItems;

    private List<PowerUpData> listOfInventoryItems = new List<PowerUpData>();
    public List<PowerUpData> InventoryItems => listOfInventoryItems;

    private bool _hasStats = false;

    /// <summary>Fired on every coin mutation (collect, spend, reset) with the new total.</summary>
    public event System.Action<int> OnCoinsChanged;

    /// <summary>Fired when speed/damage/inventory state is (re)applied - power-up purchase, rebind, reset.</summary>
    public event System.Action PlayerStatsApplied;

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

        
        // Unsubscribe from old health if any
        if (healthPlayer != null)
        {
            healthPlayer.OnHealthChanged.RemoveListener(SyncHealth);
        }
        healthPlayer = playerController.GetComponent<Health>();

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

        PlayerStatsApplied?.Invoke();
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
        OnCoinsChanged?.Invoke(currentCoins);
    }
    public bool TrySpendCoins(int amount)
    {
        if (currentCoins < amount)
        {
            return false;
        }

        currentCoins -= amount;
        Debug.Log($"Spent {amount} coins. Total: {currentCoins}");
        OnCoinsChanged?.Invoke(currentCoins);
        return true;
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
                    // No runtime effect yet - needs the meta-progression design answer (design ask).
                    break;

                case PowerUpData.PowerUpType.SpecialDamage:
                    // No runtime effect yet - needs the meta-progression design answer (design ask).
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
                    // Placeholder until design defines the spawn-modifier effect (design ask).
                    break;
                default:
                    break;
            }
        }
        PlayerStatsApplied?.Invoke();
    }
    public void ResetAllThePlayerStats()
    {
        _hasStats = false;
        currentCoins = 0;
        inventoryItems = 0;
        listOfInventoryItems.Clear();
        OnCoinsChanged?.Invoke(currentCoins);

        // Try to bind immediately if player exists
        BindToPlayer();
    }
}
