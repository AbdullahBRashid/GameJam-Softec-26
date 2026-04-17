using UnityEngine;

/// <summary>
/// A dynamic obstacle detection script for First Person Cameras.
/// Prevents the camera from "poking" through walls by raycasting from 
/// the player's center and pulling the camera back if obstructed.
/// </summary>
public class FPSCameraCollision : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("The center point of the player's head/eyes (usually a child of the player).")]
    public Transform eyePivot;
    
    [Tooltip("Layers the camera should collide with (e.g. Default, Level).")]
    public LayerMask collisionLayers = ~0; // Default to everything
    
    [Tooltip("How 'fat' the camera collision is.")]
    public float cameraRadius = 0.15f;
    
    [Tooltip("Safety margin to prevent the camera from clipping exactly on the wall edge.")]
    public float offsetFromWall = 0.05f;

    [Header("Smoothing")]
    public float smoothSpeed = 15f;

    private float _defaultDistance;
    private Vector3 _defaultLocalPos;
    private float _currentDistance;

    private void Start()
    {
        // If eyePivot isn't set, try to use the parent as center
        if (eyePivot == null) eyePivot = transform.parent;
        
        // Assume the starting position is the 'Ideal' position
        _defaultLocalPos = transform.localPosition;
        _defaultDistance = Vector3.Distance(transform.position, eyePivot.position);
        _currentDistance = _defaultDistance;
    }

    private void LateUpdate()
    {
        if (eyePivot == null) return;

        // 1. Calculate the ideal camera position in world space
        Vector3 idealWorldPos = eyePivot.position + (eyePivot.rotation * _defaultLocalPos);
        Vector3 direction = (idealWorldPos - eyePivot.position).normalized;
        float maxDist = Vector3.Distance(eyePivot.position, idealWorldPos);

        // 2. SphereCast from eyePivot towards the ideal position
        Ray ray = new Ray(eyePivot.position, direction);
        float targetDist = maxDist;

        if (Physics.SphereCast(ray, cameraRadius, out RaycastHit hit, maxDist, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            // We hit a wall! Pull the camera forward to just before the hit point
            targetDist = Mathf.Clamp(hit.distance - offsetFromWall, 0.01f, maxDist);
        }

        // 3. Smoothly move the camera distance
        _currentDistance = Mathf.Lerp(_currentDistance, targetDist, Time.deltaTime * smoothSpeed);
        
        // 4. Update the actual position
        transform.position = eyePivot.position + direction * _currentDistance;
    }
}
