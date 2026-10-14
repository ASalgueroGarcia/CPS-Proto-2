using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraRotationSystem : MonoBehaviour
{
    [Header("WAYPOINTS")]
    [SerializeField] private Transform waypointInTheCamera;
    [SerializeField] private Transform waypointFinal; // the final position for the rotation of the camera.

    [Header("LOOK AT POSITION OF THE CAMERA")]
    [SerializeField] private Transform lookAtOfTheCamera;
    [SerializeField] private Transform lookAtFinal;

    [Header("ROTATION OPTIONS")]
    [SerializeField] private float durationOfTheRotation = 2f;
    ////////////////////
    [SerializeField] private Button buttonToMoveTheCamera; // 4 the moment just a botton.
    ///////////////////
    
    private bool isMooving = false;
    private bool wPm = true;
    

    private void Start()
    {
        // the position and rotacion of the waypoint - same.
        transform.SetPositionAndRotation(waypointInTheCamera.position, GetRotation(waypointInTheCamera, lookAtOfTheCamera));
        
        // DELETE IT ON THE FUTURE.
        if (buttonToMoveTheCamera != null)
        {
            buttonToMoveTheCamera.onClick.AddListener(CameraMoovingToWaypoint);
        }
        //////////////////////////
    
    }

    public void CameraMoovingToWaypoint()
    {
        if (!isMooving)
        {
            if (wPm)
            {
                StartCoroutine(MoveToTheWaypoint(waypointFinal, lookAtFinal));
            }
            else
            {
                StartCoroutine(MoveToTheWaypoint(waypointInTheCamera, lookAtOfTheCamera));
            }
            wPm = !wPm;
        }
    }

    private IEnumerator MoveToTheWaypoint(Transform w, Transform lT)
    {
        isMooving = true;

        // Camera CURRENT position -> before mooving.
        Vector3 sPos = transform.position;

        // Camera CURRENT rotation
        Quaternion sRot = transform.rotation;

        // Camera final position - saved
        Vector3 fPos = w.position;

        // Camera final rotation
        Quaternion fRot = GetRotation(w, lT);
        

        float chrono = 0f; // chrono.
        while (chrono < durationOfTheRotation)
        {
            chrono += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(chrono / durationOfTheRotation));

            transform.position = Vector3.Lerp(sPos, fPos, t);
            transform.rotation = Quaternion.Slerp(sRot, fRot, t);
            yield return null;
        }

        transform.SetPositionAndRotation(fPos, fRot);
        isMooving = false;
    }

    private Quaternion GetRotation(Transform waypoint, Transform lookTarget)
    {
        Quaternion result = waypoint.rotation;

        if (lookTarget != null)
        {
            Vector3 dir = lookTarget.position - waypoint.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                result = Quaternion.LookRotation(dir);
            }
        }
        return result;
    }
}