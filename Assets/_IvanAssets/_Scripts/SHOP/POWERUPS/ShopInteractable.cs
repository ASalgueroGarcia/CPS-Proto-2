using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;

// SCRIPT FOR THE CHEST OF WHATEVER THAT CONTAIN THE POWERUPS.
public class ShopInteractable : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private string playerTag;
    [SerializeField] private GameObject EUI;

    private bool aInteracted = false;
    private bool playerNear = false;
    private PlayerInputManager playerInput;

    private void Start()
    {
        if(shopManager == null){
            shopManager = FindFirstObjectByType<ShopManager>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            // Player -> near? -> active.
            playerNear = true;

        // E -> UI.
        if(EUI != null && !aInteracted)
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
        }
        if (EUI != null){
            EUI.SetActive(false);
        }
    }

    private void Update()
    {
        if(playerNear && Keyboard.current.eKey.wasPressedThisFrame && !aInteracted)
        {
            shopManager.OpenShop();
            aInteracted=true;
            if(EUI != null)
            {
                EUI.SetActive(false);
            }
        }
    }
}
