using System.Collections;
using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance {get; private set;}

    [Header("CAMERA SETTINGS")]
    [SerializeField] private Transform[] cameraTargetPoints;
    [SerializeField] private float cameraSmoothSpeed = 0.6f;
    private int cIndex = 0;
    public int currentIndex => cIndex; // back to the first point.

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(SmoothMoveAp(cameraTargetPoints[0].position, cameraSmoothSpeed));
    }
    public void MoveCamera(int CameraIndex)
    {
        if (CameraIndex == cIndex) {
            return; 
        }

        if (CameraIndex < cameraTargetPoints.Length && CameraIndex >= 0)
        {
            cIndex = CameraIndex;
            StartCoroutine(SmoothMoveAp(cameraTargetPoints[CameraIndex].position, cameraSmoothSpeed));
        }
    }

    private IEnumerator SmoothMoveAp(Vector3 camera, float time)
    {
        Vector3 startPoint = transform.position; 
        float elapsed = 0f;
        while (elapsed < time)
        {
            transform.position = Vector3.Lerp(startPoint, camera, elapsed / time);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = camera;
    }
}