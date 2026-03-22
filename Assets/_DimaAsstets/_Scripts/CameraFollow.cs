using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 15, -10);
    
    [Header("Movement Settings")]
    public float smoothTime = 0.3f;
    public bool lockX = false;
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 5f; // Added a separate speed for rotation

    private Vector3 currentVelocity = Vector3.zero;

    void LateUpdate()
    {
        if (target != null)
        {
            // 1. Calculate desired position
            Vector3 desiredPosition = target.position + offset;

            // Lock X position if enabled
            if (lockX)
            {
                desiredPosition.x = offset.x;
            }

            // 2. Not! Smoothly move the camera
            // transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            transform.position = new Vector3(desiredPosition.x, desiredPosition.y, desiredPosition.z);

            // 3. Calculate rotation target (using the PLAYER's Y and Z, not the desired position)
            Vector3 lookTarget = new Vector3(transform.position.x, target.position.y, target.position.z);
            
            // 4. Smoothly rotate towards the target
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}