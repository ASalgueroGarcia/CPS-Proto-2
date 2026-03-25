using UnityEngine;

public class MapBtnBehaviour : MonoBehaviour
{
    public void ReturnToMap()
    {
        SceneController.Instance.UnloadLevel();
    }
}