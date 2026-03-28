using UnityEngine;

public class MapBtnBehaviour : MonoBehaviour
{
    [Header("Pause Menu Settings")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject eolCanvas;
    
    public void ReturnToMap()
    {
        if (SceneController.Instance)
        {
            if (pauseMenu != null && pauseMenu.activeSelf) pauseMenu.SetActive(false);
            if (eolCanvas != null && eolCanvas.activeSelf) eolCanvas.SetActive(false);
            
            // Also resume time if it was paused
            Time.timeScale = 1f;
            
            SceneController.Instance.UnloadLevel();
        }
        else
        {
            Debug.LogError("SceneController instance is null! Cannot return to map.");
        }
    }

    public void ToggleEoLCanvas()
    {
        if (eolCanvas != null)
        {
            eolCanvas.SetActive(!eolCanvas.activeSelf);
        }
    }
}