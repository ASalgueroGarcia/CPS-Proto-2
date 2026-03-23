using UnityEngine;
using System.Collections.Generic;

// PLAYER MANAGER FOR THE STATS OF THE PLAYER W THE ITEMS TOO.
public class PlayerStatsManager : MonoBehaviour
{
    [Header("HEALTH STATS")]
    [SerializeField] private float baseHealth = 100.0f; // HEALTH SCRIPT.
    private float currentHealth; // <- CHANGE....
    private float maxHealth;

    [Header("NORMAL DAMAGE STATS")]
    [SerializeField] private float baseNomalDamage = 10.0f; // SCISSORS DAMAGE.
    [SerializeField] private float baseCriticalDamage = 25.0f; 
    [SerializeField] private float baseSpecialDamage = 20.0f; 
    [SerializeField] private float baseAttackSpeed = 1.0f;
    private float currentNormalDamage;
    private float currentCriticalDamage;
    private float currentSpecialDamage;
    private float currentCritChance = 0.10f;
    private float currentAttackSpeed;

    [Header("SPEED STATS")]
    [SerializeField] private float baseSpeed = 5.0f;
    [SerializeField] private float baseDashSpeed = 30.0f;
    private float currentSpeed;
    private float currentDashSpeed;

    // How many items do u collect? ->
    private int inventoryItems = 0;
    private List<PowerUpData> listOfInventoryItems = new List<PowerUpData>();

    // REFS.
    private PlayerFSM playerFSM;
    //private PlayerHealth playerHealth;

    private void Start()
    {
        // initializations.
        currentHealth = baseHealth;
        maxHealth = baseHealth;
        currentNormalDamage = baseNomalDamage;
        currentCriticalDamage = baseCriticalDamage;
        currentSpecialDamage = baseSpecialDamage;
        currentSpeed = baseSpeed;
        currentDashSpeed = baseDashSpeed;
        currentAttackSpeed = baseAttackSpeed;

        // PLAYER CONTROLLER -> FIND.
        playerFSM = GetComponent<PlayerFSM>();
        Prints(); // PRINT ALL -> delete this, just for debugg.
    }

    public void ApplyPowerUpEffect(PowerUpData powerUp)
    {    Debug.Log("★ APPLYING POWERUP ★");  // ← VE SI LLEGA AQUÍ


        if(powerUp == null)
        {
            return;
        }
        inventoryItems++;
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
                    if (playerFSM != null){
                        playerFSM.weaponBaseDamage = currentNormalDamage;
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
                    if (playerFSM != null){
                        playerFSM.speed = currentSpeed;
                    }
                    break;

                case PowerUpData.PowerUpType.DashSpeed:
                    currentDashSpeed += effect.value;
                    if (playerFSM != null){
                        playerFSM.dashSpeed = currentDashSpeed;
                    }
                    break;

                case PowerUpData.PowerUpType.Stun:
                    break;

                // DEFENSE
                case PowerUpData.PowerUpType.Health:
                    currentHealth += effect.value;
                    maxHealth += effect.value;
                    break;

                // LOOT
                case PowerUpData.PowerUpType.enemyLoot:
                    break;

                case PowerUpData.PowerUpType.enemySpawn:
                    break;

                default:
                    break;
            }
        }
        Prints();
    }
    public void ResetAllThePlayerStats()
    {
        currentHealth = baseHealth;
        maxHealth = baseHealth;
        currentNormalDamage = baseNomalDamage;
        currentCriticalDamage = baseCriticalDamage;
        currentSpecialDamage = baseSpecialDamage;
        currentSpeed = baseSpeed;
        currentDashSpeed = baseDashSpeed;
        currentAttackSpeed = baseAttackSpeed;
        currentCritChance = 0.1f;
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