using UnityEngine;

public class MapBtnBehaviour : MonoBehaviour
{
    [Header("Pause Menu Settings")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject eolCanvas;

    private MapBehaviour mapBehaviour;

    private void Start()
    {
        mapBehaviour = FindFirstObjectByType<MapBehaviour>();
    }

    public void ReturnToMap()
    {
        if (SceneController.Instance == null)
        {
            Debug.LogError("SceneController instance is null! Cannot return to map.");
            return;
        }

        if (mapBehaviour == null)
        {
            mapBehaviour = FindFirstObjectByType<MapBehaviour>();
        }

        if (pauseMenu != null && pauseMenu.activeSelf) pauseMenu.SetActive(false);
        if (eolCanvas != null && eolCanvas.activeSelf) eolCanvas.SetActive(false);

        Time.timeScale = 1f;

        if (mapBehaviour != null)
        {
            mapBehaviour.LeaveRoom();
        }
        else
        {
            SceneController.Instance.UnloadLevel();
        }
    }
}
