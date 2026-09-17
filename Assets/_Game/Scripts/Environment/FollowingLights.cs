using UnityEngine;

public class FollowingLights : MonoBehaviour
{
    public Transform target;

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
            Vector3 direction = target.position - transform.position;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
