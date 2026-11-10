using UnityEngine;
using UnityEngine.InputSystem;

// SCRIPT FOR THE CHEST OF WHATEVER THAT CONTAIN THE POWERUPS.
public class ShopInteractable : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private string playerTag;
    [SerializeField] private GameObject EUI;

    private bool playerNear = false;
    private PlayerInputManager playerInput;

    private void Start()
    {
        if(shopManager == null){
            shopManager = FindFirstObjectByType<ShopManager>();
        }
        EUI.SetActive(false);
        InputPromptLabel.AddTo(EUI);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            // Player -> near? -> active.
            playerNear = true;
            Player.InteractableInRange = true;

        // E -> UI.
        if(EUI != null && !shopManager.IsShopOpen)
        {
            EUI.SetActive(true);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            playerNear = false;
            Player.InteractableInRange = false;
        }
        if (EUI != null){
            EUI.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (playerNear) Player.InteractableInRange = false;
    }

    private void Update()
    {
        if(playerNear && InteractPressed() && !shopManager.IsShopOpen && !Player.IsPaused)
        {
            shopManager.OpenShop();
            if(EUI != null)
            {
                EUI.SetActive(false);
            }
        }
    }

    private static bool InteractPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && keyboard.eKey.wasPressedThisFrame)
               || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);
    }
}
