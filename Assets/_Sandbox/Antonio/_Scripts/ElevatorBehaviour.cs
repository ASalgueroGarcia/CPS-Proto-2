using System.Collections;
using UnityEngine;

public class ElevatorBehaviour : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private Animator controller;
    [SerializeField] private float doorAnimTime = 1f;

    [Header("Movement Settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float speed = 2f;

    [Header("Player Settings")]
    [SerializeField] private PlayerFSM player;

    private int _currentIndex = 0;
    private bool _isBusy;

    void Start()
    {
        transform.position = waypoints[_currentIndex].position;
        OpenDoor();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_isBusy || !other.CompareTag("Player")) return;
        StartCoroutine(RideRoutine(other.transform));
    }

    private IEnumerator RideRoutine(Transform playerTransform)
    {
        _isBusy = true;
        player.enabled = false;   // now refers to the PlayerFSM field

        CloseDoor();
        yield return new WaitForSeconds(doorAnimTime);

        playerTransform.SetParent(transform);

        int target = (_currentIndex + 1) % waypoints.Length;
        Vector3 targetPos = waypoints[target].position;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        _currentIndex = target;

        playerTransform.SetParent(null);
        OpenDoor();
        yield return new WaitForSeconds(doorAnimTime);

        player.enabled = true;
        _isBusy = false;
    }

    public void OpenDoor()  => controller.SetBool("Open", true);
    public void CloseDoor() => controller.SetBool("Open", false);
}