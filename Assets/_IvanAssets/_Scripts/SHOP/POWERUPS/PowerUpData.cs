// SCRIPTABLE OBJECT FOR THE POWERUPS.
using UnityEngine;

[CreateAssetMenu(fileName = "PowerUp_", menuName = "PowerUp/Create new PowerUp")]
public class PowerUpData : ScriptableObject
{
    // --VARIABLES--
    public string powerUpName;
    public string powerUpDescription;
    public Sprite powerUpIcon;
    public float price;
    [System.Serializable]

    // EFFECT.
    public class PowerUpEffect
    {
        public PowerUpType type;
        public float value;
    }
    
    // TYPES -> TO ADD WHATEVER.
    public enum PowerUpType
    {
        // DAMAGE
        NormalDamage,
        CriticalDamage,
        SpecialDamage,
        
        // SPEED & MOVEMENT
        NormalSpeed,
        DashSpeed,

        // COMBAT
        Stun,

        // HEALTH
        Health,

        // LOOT OF THE ENEMYS / SPAWN RATES
        enemyLoot,
        enemySpawn
    }
    public PowerUpEffect[] effects;
}
