// Source - https://stackoverflow.com/a/76923442
// Posted by 11belowstudio
// Retrieved 2025-11-08, License - CC BY-SA 4.0

using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

///<summary>The clientside class </summary>
public class PlayerCamera : MonoBehaviour {
  ///<summary> the transform of the gameobject that the camera should be following</summary>
  [SerializeField]
  private Transform _target;

  [Header("Camera Distance")]
  [SerializeField] private float distance = 7f;
  [SerializeField] private float height = 2f;
  [SerializeField] private float minDistance = 3f;
  [SerializeField] private float maxDistance = 15f;

  [Header("Camera Rotation")]
  [SerializeField] private float mouseSensitivity = 0.8f;
  [SerializeField] private float minVerticalAngle = -20f;
  [SerializeField] private float maxVerticalAngle = 60f;

  [Header("Smoothing")]
  [SerializeField] private float followSpeed = 10f;
  [SerializeField] private float cameraSnapSpeed = 2f; // How fast camera snaps behind player

  [Header("Collision Prevention")]
  [SerializeField] private float groundOffset = 0.5f; // Minimum distance above ground
  [SerializeField] private float cameraRadius = 0.3f; // Sphere cast radius for smooth collision
  [SerializeField] private LayerMask collisionLayers = ~0; // What the camera collides with
  [SerializeField] private float collisionBuffer = 0.2f; // Extra space from walls

  private float currentX = 0f;
  private float currentY = 20f; // Start tilted down a bit


  ///<summary>our camera (attached to same gameobject)</summary>
  [SerializeField]
  private Camera _cam;

  //...

  void Awake() {
    _cam = GetComponent<Camera>();
    // Lock and hide cursor for better camera control

  }


  ///<summary> obtains the_target (or sets it + warps to the_target)</summary>
  public Transform Target {
    get { return _target; }
    set {
      _target = value;
      GoToTarget();
    }
  }

  ///<summary>scuffed example for getting the camera to stay with the_target object</summary>
  private void GoToTarget() {
    if(_target == null) { return; }
    // TODO: make a better implementation
    transform.position = _target.position;
    transform.rotation = _target.rotation;

    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    currentX = _target.eulerAngles.y;

  }

  private void LateUpdate() {
    if(_target == null) return;

    // Left-click, Right-click, or both buttons to rotate camera (classic MMO style)
    bool rotateCamera = Input.GetMouseButton(0) || Input.GetMouseButton(1);

    if(rotateCamera) {
      currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
      currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
      currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
    } else {
      // Only snap camera behind player when moving forward (W key held)
      bool isMovingForward = Input.GetKey(KeyCode.W);

      if(isMovingForward) {
        float playerYaw =_target.eulerAngles.y;
        float angleDifference = Mathf.DeltaAngle(currentX, playerYaw);

        // Only snap if difference is significant
        if(Mathf.Abs(angleDifference) > 5f) {
          currentX = Mathf.LerpAngle(currentX, playerYaw, cameraSnapSpeed * Time.deltaTime);
        }
      }
    }

    // Scroll wheel to zoom
    float scroll = Input.GetAxis("Mouse ScrollWheel");
    if(scroll != 0f) {
      distance -= scroll * 3f;
      distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    // Calculate ideal camera position
    Quaternion rotation = Quaternion.Euler(currentY, currentX, 0f);
    Vector3 lookAtPoint =_target.position + Vector3.up * height;
    Vector3 offsetDirection = rotation * Vector3.back;
    Vector3 idealPosition = lookAtPoint + offsetDirection * distance;

    // === COLLISION DETECTION ===

    // 1. Check for obstacles between player and camera using SphereCast
    Vector3 directionToCamera = idealPosition - lookAtPoint;
    float desiredDistance = directionToCamera.magnitude;
    Vector3 normalizedDirection = directionToCamera.normalized;

    RaycastHit hit;
    float actualDistance = desiredDistance;

    // SphereCast to detect walls/obstacles
    if(Physics.SphereCast(lookAtPoint, cameraRadius, normalizedDirection, out hit, desiredDistance, collisionLayers)) {
      // Pull camera closer if there's an obstacle
      actualDistance = Mathf.Max(hit.distance - collisionBuffer, minDistance * 0.5f);
    }

    // Apply the adjusted distance
    Vector3 adjustedPosition = lookAtPoint + normalizedDirection * actualDistance;

    // 2. Ground collision check - prevent camera from going below ground
    RaycastHit groundHit;
    if(Physics.Raycast(adjustedPosition + Vector3.up * 2f, Vector3.down, out groundHit, 100f, collisionLayers)) {
      float minAllowedHeight = groundHit.point.y + groundOffset;
      if(adjustedPosition.y < minAllowedHeight) {
        adjustedPosition.y = minAllowedHeight;
      }
    }

    // 3. Additional check: Raycast down from camera position to ensure we're not inside terrain
    if(Physics.Raycast(adjustedPosition, Vector3.down, out groundHit, groundOffset * 2f, collisionLayers)) {
      // Camera is too close to ground, push it up
      adjustedPosition.y = groundHit.point.y + groundOffset;
    }

    // Smooth follow to final position
    transform.position = Vector3.Lerp(transform.position, adjustedPosition, followSpeed * Time.deltaTime);

    // Always look at the_target
    transform.LookAt(lookAtPoint);
  }


  ///<summary>always stick with the_target</summary>
  //void Update() {
  //  GoToTarget();
  //  // ...
  //}

  // ...
}