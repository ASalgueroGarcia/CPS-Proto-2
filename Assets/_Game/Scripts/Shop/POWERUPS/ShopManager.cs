// SHOP-MANAGER -> NO SINGLETON IN CASE THERE ARE MORE STORES.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("SHOP SETTINGS")]
    [SerializeField] private Button[] powerUpsButtons = new Button[3]; // Buttons of the powerUps -> add more in the array.
    [SerializeField] private Button exitButton; // If the player do not want to buy any item.
    [SerializeField] private Button returnMapButton;
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
    public bool IsShopOpen = false;

    /// <summary>The shop on screen, if any: Esc closes it instead of opening the pause menu.</summary>
    public static ShopManager Open { get; private set; }


    private void Start()
    {
        // 1. Find the playerStatsManager.
        _playerStatsManager = PlayerStatsManager.Instance;
        _canvasG = shopPanel.GetComponent<CanvasGroup>();

        if(returnMapButton != null)
        {
            returnMapButton.gameObject.SetActive(false);
        }
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
            powerUpsButtons[j].onClick.AddListener(() => {
                if (SoundManager.Instance != null && purchaseClip != null) SoundManager.Instance.PlaySound(purchaseClip);
            });
        }
        exitButton.onClick.AddListener(() => HideShopLogic());
        // Its own listeners take the player back to the map; the shop must not freeze time again meanwhile.
        if (returnMapButton != null) returnMapButton.onClick.AddListener(() => HideShopLogic());

        foreach (Selectable selectable in shopPanel.GetComponentsInChildren<Selectable>(true))
        {
            selectable.gameObject.AddComponent<CancelClosesShop>().Shop = this;
        }

        HideShopLogic();
    }

    private void Update()
    {
        if (!IsShopOpen) return;

        // The pause menu's Resume unfreezes everything while the shop is still up.
        if (Time.timeScale > 0f)
        {
            Time.timeScale = 0f;
            Player.IsPaused = true;
        }

        KeepFocus();
    }

    // Leaving the room from the pause menu destroys an open shop: it must not leave the map frozen.
    private void OnDisable()
    {
        if (IsShopOpen) HideShopLogic();
    }

    // A gamepad can only move from a selected control. A mouse click on the background clears the
    // selection, and so does the pause menu closing.
    private void KeepFocus()
    {
        EventSystem events = EventSystem.current;
        if (events == null) return;

        GameObject selected = events.currentSelectedGameObject;
        if (selected != null && selected.activeInHierarchy) return;

        Button first = powerUpsButtons.Length > 0 && powerUpsButtons[0] != null ? powerUpsButtons[0] : exitButton;
        events.SetSelectedGameObject(first.gameObject);
    }

    /// <summary>The UI Cancel input (Esc, gamepad B) only reaches the selected control.</summary>
    private class CancelClosesShop : MonoBehaviour, ICancelHandler
    {
        public ShopManager Shop;

        public void OnCancel(BaseEventData eventData) => Shop.HideShopLogic();
    }
    
    public void OpenShop()
    {
        StopAllCoroutines();
        shopPanel.SetActive(true);
        IsShopOpen = true;
        returnMapButton.gameObject.SetActive(true);
    
        //to check if the player has the item already ---
        List<PowerUpData> availablePowerUps = new List<PowerUpData>();
        foreach(PowerUpData p in powerUpsA)
        {
            if (!_playerStatsManager.InventoryItems.Contains(p))
            {
                availablePowerUps.Add(p);
            }
        }

        // Select 3 random items in the list.
        List<int> iRandom = new List<int>();

        while(iRandom.Count < availablePowerUps.Count && iRandom.Count < 3)
        {
            int indexR = UnityEngine.Random.Range(0, availablePowerUps.Count);  
            if(!iRandom.Contains(indexR)){
                iRandom.Add(indexR);
            }
        }

        for(int i = 0; i < iRandom.Count; i++)
        {
            _currentPowerUps[i] = availablePowerUps[iRandom[i]];
            
            itemNameTexts[i].text = _currentPowerUps[i].powerUpName;
            itemDescriptionTexts[i].text = _currentPowerUps[i].powerUpDescription;
            itemPriceTexts[i].text = "$" + _currentPowerUps[i].price.ToString();
            powerUpImage[i].sprite = _currentPowerUps[i].powerUpIcon;
        }
        
        Time.timeScale = 0f;
        Player.IsPaused = true;
        Open = this;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        KeepFocus();
    }

    public void HideShopLogic()
    {
        StopAllCoroutines();
        if (IsShopOpen) Player.IsPaused = false;
        if (Open == this) Open = null;
        if (shopPanel != null) shopPanel.SetActive(false);
        if (returnMapButton != null) returnMapButton.gameObject.SetActive(false);
        IsShopOpen = false;
        Time.timeScale = 1f;
    }

    public void PowerUpSelect(int l)
    {
        PowerUpData selectedPowerUp = _currentPowerUps[l];
        // APPLY THE POWERUP.
        if (_playerStatsManager != null && selectedPowerUp != null)
        {
            _playerStatsManager.ApplyPowerUpEffect(selectedPowerUp);
        }
        HideShopLogic();
    }
}