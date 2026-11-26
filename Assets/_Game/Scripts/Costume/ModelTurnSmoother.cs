using UnityEngine;

/// <summary>
/// The player snaps to face the input direction. This turns the model after it over a short time instead, so an
/// instant 180° doesn't whip the scarf's cloth through the body. Visual only: movement, hitboxes and attacks keep
/// the player's own (snapped) facing.
/// </summary>
public class ModelTurnSmoother : MonoBehaviour
{
    [Tooltip("Seconds the model takes to follow a 180° turn of the player (0 = snap with it).")]
    [SerializeField, Min(0f)] private float turnTime = 0.1f;

    private Quaternion restLocalRotation;
    private Quaternion rotation;

    private void Awake()
    {
        restLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        rotation = TargetRotation;
    }

    private void LateUpdate()
    {
        Quaternion target = TargetRotation;
        rotation = turnTime > 0f ? Quaternion.RotateTowards(rotation, target, 180f / turnTime * Time.deltaTime) : target;
        transform.rotation = rotation;
    }

    private Quaternion TargetRotation => transform.parent != null ? transform.parent.rotation * restLocalRotation : restLocalRotation;
}
