using UnityEngine;

public class MapBtnBehaviour : MonoBehaviour
{
    [Header("Pause Menu Settings")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject eolCanvas;

    private MapBehaviour _mapBehaviour;

    private void Start()
    {
        _mapBehaviour = FindFirstObjectByType<MapBehaviour>();
    }

    public void ReturnToMap()
    {
        if (SceneController.Instance)
        {
            // Only a finished room (its end-of-level screen is up) counts as completed. The pause
            // menu's button has no eolCanvas: leaving through it must not clear the room, or
            // walking out of the boss room would save a victory.
            bool roomFinished = eolCanvas != null && eolCanvas.activeSelf;

            if (pauseMenu != null && pauseMenu.activeSelf) pauseMenu.SetActive(false);
            if (roomFinished) eolCanvas.SetActive(false);
            
            Time.timeScale = 1f;
            if (roomFinished) _mapBehaviour.CompletedNode();
            SceneController.Instance.UnloadLevel();
        }
        else
        {
            Debug.LogError("SceneController instance is null! Cannot return to map.");
        }
    }
}