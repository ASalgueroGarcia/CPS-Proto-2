using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The theatre poster in the lobby / shop. Walk up to it and press Interact (E / North button)
/// to open the act select screen.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ActPoster : MonoBehaviour
{
    [Tooltip("Left empty, the first ActSelectScreen in the loaded scenes is used.")]
    [SerializeField] private ActSelectScreen actSelectScreen;
    [SerializeField] private GameObject interactPrompt;

    private bool _playerNear;

    private void Start()
    {
        if (actSelectScreen == null)
        {
            actSelectScreen = FindFirstObjectByType<ActSelectScreen>(FindObjectsInactive.Include);
        }

        if (actSelectScreen == null)
        {
            Debug.LogError("[ActPoster] No ActSelectScreen found - the poster can't open.", this);
        }

        SetPrompt(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) _playerNear = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) _playerNear = false;
    }

    private void Update()
    {
        bool canOpen = _playerNear && actSelectScreen != null && !ActSelectScreen.IsOpen && !PlayerFSM.IsPaused;
        SetPrompt(canOpen);

        if (canOpen && InteractPressed())
        {
            SetPrompt(false);
            actSelectScreen.Open();
        }
    }

    private static bool InteractPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && keyboard.eKey.wasPressedThisFrame)
               || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);
    }

    private void SetPrompt(bool visible)
    {
        if (interactPrompt != null && interactPrompt.activeSelf != visible)
        {
            interactPrompt.SetActive(visible);
        }
    }
}
