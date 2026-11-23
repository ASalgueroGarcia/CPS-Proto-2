 using UnityEngine;
using UnityEngine.InputSystem;

public class ShopLobbyInteractable : MonoBehaviour
{
    [SerializeField] private ShopZoom zoom;
    [SerializeField] private GameObject EUI;

    private bool playerNear = false;

    private void Start()
    {
        if (EUI != null) {
            EUI.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider o)
    {
        if (o.CompareTag("Player"))
        {
            playerNear = true;
            if (EUI != null && !zoom.zoomed)
            {
             EUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerNear = false;
        }

        if(EUI != null) {
            EUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerNear && Keyboard.current.eKey.wasPressedThisFrame && !zoom.zoomed)
        {
            zoom.ZoomIn();
            if (EUI != null) EUI.SetActive(false);
        }
    }
}