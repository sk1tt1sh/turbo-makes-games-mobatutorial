using UnityEngine;

/// <summary>
/// Makes UI elements (like health bars) always face the camera.
/// Useful when switching from top-down to third-person view.
/// </summary>
public class BillboardToCamera : MonoBehaviour
{
    [Header("Billboard Settings")]
    [Tooltip("If true, billboard will face camera. If false, billboard will face opposite direction.")]
    public bool faceCamera = true;
    
    [Tooltip("Lock Y rotation to keep health bar upright")]
    public bool lockYRotation = true;

    private Camera _mainCamera;

    void Start()
    {
        // Cache the main camera
        _mainCamera = Camera.main;
        
        if (_mainCamera == null)
        {
            Debug.LogWarning("[BillboardToCamera] No main camera found! Health bars won't billboard.");
        }
    }

    void LateUpdate()
    {
        if (_mainCamera == null)
        {
            // Try to find camera again if it wasn't found on Start
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        // Calculate direction from this object to camera
        Vector3 directionToCamera = _mainCamera.transform.position - transform.position;
        
        // Make this object look at the camera
        if (faceCamera)
        {
            // Look at camera position
            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
        else
        {
            // Face away from camera (opposite direction)
            transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }

        // Optional: Lock Y rotation to keep health bar upright
        if (lockYRotation)
        {
            Vector3 euler = transform.eulerAngles;
            euler.z = 0f; // Keep it upright (no roll)
            transform.eulerAngles = euler;
        }
    }
}
