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
            if (pauseMenu.activeSelf) pauseMenu.SetActive(!pauseMenu.activeSelf);
            if (eolCanvas.activeSelf) ToggleEoLCanvas();
            SceneController.Instance.UnloadLevel();
        }
        else
        {
            Debug.LogError("SceneController instance is null! Cannot return to map.");
        }
    }

    public void ToggleEoLCanvas()
    {
        eolCanvas.SetActive(!eolCanvas.activeSelf);
    }
}