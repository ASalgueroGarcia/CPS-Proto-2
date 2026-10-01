using UnityEngine;

/// <summary>
/// Scissors-specific data for a single combo step. Authored in the Inspector
/// via the <c>_comboSteps</c> array on <see cref="ScissorsWeapon"/>.
/// </summary>
[System.Serializable]
public struct ScissorsComboResult
{
    [Tooltip("Multiplier applied to weaponBaseDamage (1.0 = base damage).")]
    public float DamageMultiplier;

    [Tooltip("Bonus added to baseCritChance (e.g. 0.25 = +25% crit chance).")]
    public float CritBonus;

    public float Knockback;
    public Color Color;
    public string AnimState;
    public int HitboxIndex;
    public bool UseRandomLightClip;
    public AudioClip AudioClip;
}
