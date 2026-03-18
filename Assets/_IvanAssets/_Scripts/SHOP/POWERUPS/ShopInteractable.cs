using UnityEngine;
using UnityEngine.InputSystem;

// SCRIPT FOR THE CHEST OF WHATEVER THAT CONTAIN THE POWERUPS.
public class ShopInteractable : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    private PlayerInputManager playerInput;

    private void Start()
    {
        if(shopManager == null){
            shopManager = FindFirstObjectByType<ShopManager>();
        }
    }


    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            shopManager.OpenShop();
        }
    }
}
