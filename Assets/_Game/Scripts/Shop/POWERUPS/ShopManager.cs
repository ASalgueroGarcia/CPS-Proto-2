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
    [SerializeField] private List<PowerUpData> powerUpsA = new List<PowerUpData>();

    [Header("SFX Settings")]
    [SerializeField] private AudioClip purchaseClip;

    private PlayerStatsManager _playerStatsManager;
    private readonly PowerUpData[] _currentPowerUps = new PowerUpData[3];
    private CanvasGroup _canvasG;
    private RectTransform _panelRect;
    private UIManager _uiManager;

    // methods.
    private void Start()
    {
        // 1. Find the playerStatsManager.
        _playerStatsManager = PlayerStatsManager.Instance;
        _canvasG = shopPanel.GetComponent<CanvasGroup>();
        _uiManager = FindFirstObjectByType<UIManager>();

        if (_canvasG == null)
        {
            _canvasG = shopPanel.AddComponent<CanvasGroup>();
        }
        _panelRect = shopPanel.GetComponent<RectTransform>();

        // 2. Conect the buttont to -> selectPower.
        for(int j = 0; j < powerUpsButtons.Length; j++)
        {
            int f = j;
            powerUpsButtons[j].onClick.AddListener(() => PowerUpSelect(f));
            powerUpsButtons[j].onClick.AddListener(() => SoundManager.Instance.PlaySound(purchaseClip));
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

        while(iRandom.Count < powerUpsA.Count && iRandom.Count < 3)
        {
            int indexR = UnityEngine.Random.Range(0, powerUpsA.Count);  
            if(!iRandom.Contains(indexR)){
                iRandom.Add(indexR);
            }
        }

        Debug.Log($"[Shop] OpenShop: populating {iRandom.Count} slot(s) from {powerUpsA.Count} item(s) - manager #{GetInstanceID()}, panel '{(shopPanel != null ? shopPanel.name : "NULL")}'");
        int failingSlot = -1;
        try
        {
            for(int i = 0; i < iRandom.Count; i++)
            {
                failingSlot = i;
                PowerUpData item = powerUpsA[iRandom[i]];
                Debug.Log($"[Shop] slot {i} <- item #{iRandom[i]} {(item != null ? item.powerUpName : "NULL (missing PowerUpData asset?)")}");

                _currentPowerUps[i] = item;

                itemNameTexts[i].text = item != null ? item.powerUpName : "";
                itemDescriptionTexts[i].text = item != null ? item.powerUpDescription : "";
                itemPriceTexts[i].text = item != null ? "$" + item.price.ToString() : "$0";
                powerUpImage[i].sprite = item != null ? item.powerUpIcon : null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Shop] OpenShop population FAILED at slot {failingSlot} (the last '[Shop] slot' line above names the culprit).", this);
            Debug.LogException(ex);
        }

        Time.timeScale = 0f;
        PlayerFSM.IsPaused = true;
    }

    public void HideShopLogic()
    {
        StopAllCoroutines();
        shopPanel.SetActive(false);
        Time.timeScale = 1f;
        PlayerFSM.IsPaused = false;
    }

    public void PowerUpSelect(int l)
    {
        PowerUpData selectedPowerUp = _currentPowerUps[l];
        if (selectedPowerUp == null)
        {
            Debug.Log($"[Shop] Slot {l} is empty - open the shop first (OpenShop fills the slots).");
            return;
        }

        if (_playerStatsManager == null || !_playerStatsManager.TrySpendCoins((int)selectedPowerUp.price))
        {
            Debug.Log($"[Shop] Not enough coins for {selectedPowerUp.powerUpName} (${selectedPowerUp.price:0}).");
            return;
        }

        _playerStatsManager.ApplyPowerUpEffect(selectedPowerUp);
        Debug.Log($"[Shop] Purchased {selectedPowerUp.powerUpName} for ${selectedPowerUp.price:0}.");
        HideShopLogic();
        _uiManager.ShowEoLCanvas();
    }
}