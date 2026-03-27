using UnityEngine;

public class MapBtnBehaviour : MonoBehaviour
{
    public void ReturnToMap()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.UnloadLevel();
        }
        else
        {
            Debug.LogError("SceneController instance is null! Cannot return to map.");
        }
    }
}