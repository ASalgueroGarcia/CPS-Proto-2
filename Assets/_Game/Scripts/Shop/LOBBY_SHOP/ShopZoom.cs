using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class ShopZoom : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button exitButton;

    [Header("CAMERA")]
    [SerializeField] private MonoBehaviour playerScript; // to bloq the player movement.
    [SerializeField] private int cameraIndex;

    private int cameraIndexPrev;
    public bool zoomed = false;


    public void Start()
    {
        exitButton.gameObject.SetActive(false);
        exitButton.onClick.AddListener(ZoomOut);
    }
    public void ZoomIn()
    {
        if(zoomed){
            return;
        }
        zoomed = true;

        cameraIndexPrev = CameraSystem.Instance.currentIndex;
        CameraSystem.Instance.MoveCamera(cameraIndex);

        if (playerScript != null) playerScript.enabled = false;
        if (exitButton != null) exitButton.gameObject.SetActive(true);
    }

    public void ZoomOut()
    {
        if (!zoomed) {
            return;
        }

        CameraSystem.Instance.MoveCamera(cameraIndexPrev);

        if (playerScript != null) playerScript.enabled = true;
        if (exitButton != null) exitButton.gameObject.SetActive(false);

        zoomed = false;
    }
}