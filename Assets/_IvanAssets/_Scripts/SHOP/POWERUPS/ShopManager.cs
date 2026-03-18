// SHOP-MANAGER -> NO SINGLETON IN CASE THERE ARE MORE STORES.
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using System;

public class ShopManager : MonoBehaviour
{
    [Header("SHOP SETTINGS")]
    [SerializeField] private Button[] powerUpsButtons; // Buttons of the powerUps.
    [SerializeField] private GameObject shopPanel;
    //[SerializeField] private Text[] powerUpsNames;
    //[SerializeField] private Image[] powerUpImage;
    [SerializeField] private List<PowerUpData> PowerUpsA = new List<PowerUpData>();
    private PlayerInputManager playermanager;

   /* // ANIMATION -> OPENING THE SHOP.
    [Header("SHOP SETTINGS")]
    [SerializeField] private float animTime = 1f;
    
*/

    // methods.
    private void Start()
    {
        // 1. Find the playerManager.
        playermanager = FindFirstObjectByType<PlayerInputManager>();

        // 2. Conect the buttont to -> selectPower.
        for(int j = 0; j < powerUpsButtons.Length; j++)
        {
            int f = j;
            powerUpsButtons[j].onClick.AddListener(() => PowerUpSelect(f));
        }
        HideShopLogic();
    }
    
    public void OpenShop()
    {
        // Shop active.
        shopPanel.SetActive(true);

        for(int i = 0; i < powerUpsButtons.Length; i++)
        {
            if(i < PowerUpsA.Count)
            {
                PowerUpData powerUp = PowerUpsA[i];
                //powerUpsNames[i].text = powerUp.powerUpName;
                //powerUpImage[i].sprite = powerUp.powerUpIcon;
            }
        }
        // GAME PAUSE.
        Time.timeScale = 0f;
    }

    public void HideShopLogic()
    {
        // Shop hide.
        shopPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void PowerUpSelect(int l)
    {
        PowerUpData selectedPowerUp = PowerUpsA[l];
        // APPLY THE POWERUP.
        if (playermanager != null)
        {
           // playermanager.Apply(selectedPowerUp); // apply the power.
        }
        HideShopLogic();
    }
}