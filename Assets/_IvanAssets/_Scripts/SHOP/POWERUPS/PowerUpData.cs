// SCRIPTABLE OBJECT FOR THE POWERUPS.
using UnityEngine;

[CreateAssetMenu(fileName = "PowerUp_", menuName = "PowerUp/Create new PowerUp")]
public class PowerUpData : ScriptableObject
{
    // --VARIABLES--
    public string powerUpName;
    public string powerUpDescription;
    public Sprite powerUpIcon;

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
        Damage,
        Burn,
        Speed,
        Invisibility
    }
    public PowerUpEffect[] effects;
}
