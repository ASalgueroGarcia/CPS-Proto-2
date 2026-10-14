using UnityEngine;

public class CameraTriggers : MonoBehaviour
{
    public int nextCamerPosition;
    public int prevCameraPosition;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 playerPosition = other.transform.position; // The Player P.
            Vector3 cameraTriggerPosition = transform.position;

            // Player -> left of the trigger -> nextIndex.
            // Player -> left of the trigger -> prevIndex.
            if (playerPosition.x < cameraTriggerPosition.x){
                CameraSystem.Instance.MoveCamera(nextCamerPosition);
            }

            else{
                CameraSystem.Instance.MoveCamera(prevCameraPosition);
            }
        }
    }
}