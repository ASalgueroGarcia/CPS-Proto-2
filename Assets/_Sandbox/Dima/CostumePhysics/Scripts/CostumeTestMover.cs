using System.Collections;
using UnityEngine;

// Sandbox only: drives a test character through a fixed, looping course so the Unity Cloth
// and JigglePhysics copies are compared on identical motion. Mirrors PlayerFSM: same speeds,
// facing snaps instantly, and the Animator gets the same IsMoving bool.
public class CostumeTestMover : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    [Header("Speeds (PlayerFSM defaults)")]
    [SerializeField] private float walkSpeed = 14f;
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashDuration = 0.2f;
    [Tooltip("Multiplies every speed, for a test model whose size differs from Main_Char.")]
    [SerializeField] private float speedScale = 1f;

    [Header("Course timings (seconds)")]
    [SerializeField] private float idleTime = 1.5f;
    [SerializeField] private float walkTime = 1f;
    [SerializeField] private float stopTime = 1f;

    [Header("References")]
    [SerializeField] private Animator animator;

    private Vector3 homePosition;
    private Quaternion homeRotation;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    private void OnEnable()
    {
        StartCoroutine(RunCourse());
    }

    private IEnumerator RunCourse()
    {
        while (true)
        {
            yield return Hold(idleTime);                   // settle at rest
            yield return Move(walkSpeed, walkTime);        // start from rest
            yield return Hold(stopTime);                   // sudden stop
            transform.Rotate(0f, 180f, 0f);                // instant turn, like PlayerFSM's facing snap
            yield return Move(walkSpeed, walkTime);
            yield return Move(dashSpeed, dashDuration);    // dash straight out of a walk
            yield return Hold(stopTime);
            transform.SetPositionAndRotation(homePosition, homeRotation); // teleport, like a room change
        }
    }

    private IEnumerator Hold(float duration)
    {
        SetMoving(false);
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator Move(float speed, float duration)
    {
        SetMoving(true);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            transform.position += transform.forward * (speed * speedScale * Time.deltaTime);
            yield return null;
        }
    }

    private void SetMoving(bool moving)
    {
        if (animator != null) animator.SetBool(IsMovingHash, moving);
    }
}
