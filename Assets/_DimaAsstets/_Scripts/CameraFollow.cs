using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 15, -10);
    public float smoothSpeed = 0.125f;

    [Header("Settings")]
    public bool lockX = true;

    void LateUpdate()
    {
        if (target != null)
        {
            // Calculate desired position based on target and offset
            Vector3 desiredPosition = target.position + offset;

            // Lock X position if enabled, keeping it at the offset's X value
            if (lockX)
            {
                desiredPosition.x = offset.x;
            }

            // Smoothly interpolate to the desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // Maintain a fixed look at the target (or a point relative to it)
            transform.LookAt(new Vector3(target.position.x, target.position.y, target.position.z));
        }
    }
}