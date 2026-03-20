using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    public Vector3 offset = new Vector3(0, 15, -10);

    // public float smoothSpeed = 0.125f;
    public Vector3 currentVelocity = Vector3.zero;
    public float smoothTime = 0.3f;


    [Header("Settings")]
    public bool lockX = false; // we should damp it too, so it doesn't move all the way with the player
    // the camera sees part of the scene and the x of the camera changes just enough to cover the rest of the scene

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

            transform.position =
                Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);


            // Smoothly interpolate to the desired position
            // Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            // transform.position = smoothedPosition;


            Vector3 relativePos = target.position - transform.position;
            if (relativePos!= Vector3.zero){
                Quaternion targetRotation = Quaternion.LookRotation(relativePos);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothTime);
                // transform.LookAt(new Vector3(target.position.x, target.position.y, target.position.z));
            }}
    }
}