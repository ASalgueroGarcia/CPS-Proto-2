using System;
using Unity.VisualScripting;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 15, -10);

    [Header("Movement Settings")] public float smoothTime = 0.09f;
    public bool lockX = false;

    [Header("Boundary Settings")] public Transform leftWall;
    public Transform rightWall;
    public Transform backWall;
    public Transform frontWall;

    [Header("Rotation Settings")] public float rotationSpeed = 5f; // Added a separate speed for rotation

    private Vector3 currentVelocity = Vector3.zero;

    private void Start()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void LateUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            // 1. Calculate desired position
            Vector3 desiredPosition = target.position + offset;

            // Lock X position if enabled
            if (lockX)
            {
                desiredPosition.x = offset.x;
            }

            // Clamp X position independently
            if (leftWall != null && rightWall != null)
            {
                float minX = Mathf.Min(leftWall.position.x, rightWall.position.x);
                float maxX = Mathf.Max(leftWall.position.x, rightWall.position.x);
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            }

            // Clamp Z position independently (including offset so camera stays at the same distance)
            if (backWall != null && frontWall != null)
            {
                float minZ = Mathf.Min(backWall.position.z, frontWall.position.z) + offset.z;
                float maxZ = Mathf.Max(backWall.position.z, frontWall.position.z) + offset.z;
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, minZ, maxZ);
            }


            // 2. Smoothly move the camera
            transform.position =
                Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            // transform.position = new Vector3(desiredPosition.x, desiredPosition.y, desiredPosition.z);

            // 3. Calculate rotation target (using the PLAYER's Y and Z, not the desired position)
            Vector3 lookTarget = new Vector3(transform.position.x, target.position.y, target.position.z);

            // 4. Smoothly rotate towards the target
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
            transform.rotation =
                Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}