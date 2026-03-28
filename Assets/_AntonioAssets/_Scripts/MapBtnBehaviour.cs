using UnityEngine;

public class MapBtnBehaviour : MonoBehaviour
{
    [Header("Pause Menu Settings")]
    [SerializeField] private GameObject pauseMenu;
    
    public void ReturnToMap()
    {
        if (SceneController.Instance)
        {
            if (pauseMenu.activeSelf) pauseMenu.SetActive(!pauseMenu.activeSelf);
            SceneController.Instance.UnloadLevel();
        }
        else
        {
            Debug.LogError("SceneController instance is null! Cannot return to map.");
        }
    }
}