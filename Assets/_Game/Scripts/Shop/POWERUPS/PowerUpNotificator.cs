using UnityEngine;
using System.Collections.Generic;

public class PowerUpNotificator : MonoBehaviour
{
    public static PowerUpNotificator InstanceP;

    // lIst of observers.
    private List<IPowerUp> _obs = new List<IPowerUp>();
    private void Awake()
    {
        if (InstanceP != null && InstanceP != this)
        {
            Destroy(gameObject);
            return;
        }
        InstanceP = this;
    }

    public void Attach(IPowerUp obs)
    {
        if(obs == null)return;
        Debug.Log("Observer Attached: " + obs);
       _obs.Add(obs);
    }
    public void Detach(IPowerUp obs)
    {
        if(obs ==null)return;
        Debug.Log("Observer Detached: " + obs);
        _obs.Remove(obs);
    }

    public void Notify(PowerUpData data)
    {
        /* 
        1. POSIBLE CHANGE - FOR - COUNT -1 TO 0 -> SECURITY.

        for (int i = _observers.Count - 1; i >= 0; i--)
        {
            _observers[i].OnPowerUpBuy(data);
        }
        */

        Debug.Log("Notifying"); 
        for(int i = 0; i < _obs.Count; i++)
        {
            _obs[i].OnPowerUpBuy(data);
        }
    }
}
