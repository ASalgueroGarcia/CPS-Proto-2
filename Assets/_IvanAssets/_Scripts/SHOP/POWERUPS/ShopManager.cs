// SHOP-MANAGER -> NO SINGLETON IN CASE THERE ARE MORE STORES.
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("SHOP SETTINGS")]
    [SerializeField] private Button[] powerUpsButtons = new Button[3]; // Buttons of the powerUps -> add more in the array.
    [SerializeField] private Button exitButton; // If the player do not want to buy any item.
    [SerializeField] private GameObject shopPanel;

    [Header("UI ELEMENTS PER ITEM")]
    [SerializeField] private TextMeshProUGUI[] itemNameTexts = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI[] itemDescriptionTexts = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI[] itemPriceTexts = new TextMeshProUGUI[3];
    [SerializeField] private Image[] powerUpImage = new Image[3];
    [SerializeField] private List<PowerUpData> PowerUpsA = new List<PowerUpData>();


    private PlayerStatsManager playerStatsManager;
    private PowerUpData[] currentPowerUps = new PowerUpData[3];
    private CanvasGroup canvasG;
    private RectTransform panelRect;
    private UIManager _uiManager;

    // methods.
    private void Start()
    {
        // 1. Find the playerStatsManager.
        playerStatsManager = FindFirstObjectByType<PlayerStatsManager>();
        canvasG = shopPanel.GetComponent<CanvasGroup>();
        _uiManager = FindFirstObjectByType<UIManager>();

        if (canvasG == null)
        {
            canvasG = shopPanel.AddComponent<CanvasGroup>();
        }
        panelRect = shopPanel.GetComponent<RectTransform>();

        // 2. Conect the buttont to -> selectPower.
        for(int j = 0; j < powerUpsButtons.Length; j++)
        {
            int f = j;
            powerUpsButtons[j].onClick.AddListener(() => PowerUpSelect(f));
        }
        exitButton.onClick.AddListener(() => HideShopLogic());
        HideShopLogic();
    }
    
    public void OpenShop()
    {
        StopAllCoroutines();
        shopPanel.SetActive(true);

        // Select 3 random items in the list.
        List<int> iRandom = new List<int>();

        while(iRandom.Count < PowerUpsA.Count && iRandom.Count < 3)
        {
            int indexR = UnityEngine.Random.Range(0, PowerUpsA.Count);  
            if(!iRandom.Contains(indexR)){
                iRandom.Add(indexR);
            }
        }

        for(int i = 0; i < iRandom.Count; i++)
        {
            currentPowerUps[i] = PowerUpsA[iRandom[i]];
            
            itemNameTexts[i].text = currentPowerUps[i].powerUpName;
            itemDescriptionTexts[i].text = currentPowerUps[i].powerUpDescription;
            itemPriceTexts[i].text = "$" + currentPowerUps[i].price.ToString();
            powerUpImage[i].sprite = currentPowerUps[i].powerUpIcon;
        }
        
        Time.timeScale = 0f;
    }

    public void HideShopLogic()
    {
        StopAllCoroutines();
        shopPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void PowerUpSelect(int l)
    {
        PowerUpData selectedPowerUp = currentPowerUps[l];
        // APPLY THE POWERUP.
        if (playerStatsManager != null && selectedPowerUp != null)
        {
            playerStatsManager.ApplyPowerUpEffect(selectedPowerUp);
        }
        HideShopLogic();
        _uiManager.ShowEoLCanvas();
    }
}