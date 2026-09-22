using UnityEngine;

public abstract class PowerUpConcreteBase : MonoBehaviour, IPowerUp
{
    [Header("POWERUPS SETTINGS")]
    [SerializeField] private PowerUpData.PowerUpType type;

    protected virtual void OnEnable()
    {
        PowerUpNotificator.InstanceP.Attach(this);

    }

    protected virtual void OnDisable()
    {
        if (PowerUpNotificator.InstanceP != null)
        {
            PowerUpNotificator.InstanceP.Detach(this);
        }
    }

public void OnPowerUpBuy(PowerUpData data)
{
    for (int i = 0; i < data.effects.Length; i++)
    {
        PowerUpData.PowerUpEffect effect = data.effects[i];

        if (effect.type == type)
        {
            Apply(effect.value);
        }
    }
}
    protected abstract void Apply(float value);
}