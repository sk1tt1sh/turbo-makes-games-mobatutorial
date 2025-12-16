using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Smoothly follows DOTS player entity for Cinemachine camera target.
/// Handles the timing mismatch between DOTS tick rate and visual frame rate.
/// </summary>
public class CameraTargetFollowLocalChamp : MonoBehaviour {
  [Header("Offset relative to the champ position")]
  public Vector3 offset = new Vector3(0f, 1.6f, 0f);

  [Header("Smoothing - INCREASE this to reduce jank")]
  [Range(0f, 30f)]
  public float positionSmoothing = 15f; // Higher = smoother but more lag

  [Range(0f, 30f)]
  public float rotationSmoothing = 10f; // Smooth rotation too

  EntityManager _em;
  EntityQuery _localChampQuery;
  Entity _cachedChamp;

  // Store previous position for better interpolation
  private Vector3 _targetPosition;
  private Quaternion _targetRotation;

  void Start() {
    if(World.DefaultGameObjectInjectionWorld == null) return;

    _em = World.DefaultGameObjectInjectionWorld.EntityManager;

    // OwnerChampTag is what your input system already relies on for "this is my champ"
    _localChampQuery = _em.CreateEntityQuery(
        ComponentType.ReadOnly<OwnerChampTag>(),
        ComponentType.ReadOnly<LocalTransform>()
    );

    // Initialize target position
    _targetPosition = transform.position;
    _targetRotation = transform.rotation;
  }

  void LateUpdate() {
    // Check if world and entity manager still exist
    if(World.DefaultGameObjectInjectionWorld == null) return;

    // Reacquire if needed
    if(_cachedChamp == Entity.Null || !_em.Exists(_cachedChamp)) {
      if(_localChampQuery.IsEmptyIgnoreFilter) return;
      _cachedChamp = _localChampQuery.GetSingletonEntity();
    }

    // Get DOTS entity transform
    var lt = _em.GetComponentData<LocalTransform>(_cachedChamp);

    // Calculate desired position (DOTS position + offset)
    Vector3 desiredPosition = (Vector3)lt.Position + offset;
    Quaternion desiredRotation = lt.Rotation;

    // Smooth interpolation to reduce jank
    if(positionSmoothing > 0f) {
      _targetPosition = Vector3.Lerp(_targetPosition, desiredPosition, Time.deltaTime * positionSmoothing);
    }
    else {
      _targetPosition = desiredPosition; // Instant snap if smoothing = 0
    }

    if(rotationSmoothing > 0f) {
      _targetRotation = Quaternion.Slerp(_targetRotation, desiredRotation, Time.deltaTime * rotationSmoothing);
    }
    else {
      _targetRotation = desiredRotation;
    }

    // Apply to this GameObject
    transform.position = _targetPosition;
    transform.rotation = _targetRotation;
  }
}
