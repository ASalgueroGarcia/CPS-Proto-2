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
            if (pauseMenu != null && pauseMenu.activeSelf) pauseMenu.SetActive(false);
            if (eolCanvas != null && eolCanvas.activeSelf) eolCanvas.SetActive(false);
            
            Time.timeScale = 1f;
            _mapBehaviour.CompletedNode();
            SceneController.Instance.UnloadLevel();
        }
        else
        {
            Debug.LogError("SceneController instance is null! Cannot return to map.");
        }
    }
}