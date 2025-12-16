// ChampMoveInputSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WASD camera-relative movement input for DOTS characters.
/// Replaces Troy's click-to-move system with MMO-style WASD controls.
/// </summary>
[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class ChampMoveInputSystem : SystemBase {
  MobaInputActions _inputActions;
  CollisionFilter _selectionFilter;

  protected override void OnCreate() {
    RequireForUpdate<OwnerChampTag>();

    _inputActions = new MobaInputActions();
    //This maps to the physics category names
    _selectionFilter = new CollisionFilter {
      BelongsTo = 1 << 5, //Raycasts group 
      CollidesWith = 1 << 1 | 1 << 2 | 1 << 4//Champs Minions Structures
    };
  }

  protected override void OnStartRunning() {
    _inputActions.Enable();
    _inputActions.GameplayMap.SelectMovePosition.performed += OnAutoAttackTargetSelect;
  }

  protected override void OnStopRunning() {
    _inputActions.Disable();
  }

  private void OnAutoAttackTargetSelect(InputAction.CallbackContext obj) {
    if(!SystemAPI.TryGetSingletonEntity<OwnerChampTag>(out Entity champEntity))
      return;

    var currentTransform = EntityManager.GetComponentData<Unity.Transforms.LocalTransform>(champEntity);

    CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
    RaycastInput selectionInput = GetRayCastInput(ref collisionWorld, currentTransform);
    Debug.Log("OnAutoAttackTargetSelect");
    
    if(collisionWorld.CastRay(selectionInput, out var closestHit)) {
      Debug.Log("Rightclick raycast hit!");
      SetAutoAttackTargetEntity(closestHit);
    }
  }

  protected override void OnUpdate() {
    // Only process input for the local player's champion
    if(!SystemAPI.TryGetSingletonEntity<OwnerChampTag>(out Entity champEntity))
      return;

    // Get camera for camera-relative movement
    Camera mainCamera = Camera.main;
    if(mainCamera == null) {
      Debug.LogWarning("No main camera found for WASD input!");
      return;
    }

    // Read WASD input
    float forward = 0f;
    float strafe = 0f;

    if(Input.GetKey(KeyCode.W)) forward += 1f;
    if(Input.GetKey(KeyCode.S)) forward -= 1f;
    if(Input.GetKey(KeyCode.A)) strafe -= 1f;
    if(Input.GetKey(KeyCode.D)) strafe += 1f;

    // Get current position (needed both for movement and for STOP)
    var currentTransform = EntityManager.GetComponentData<Unity.Transforms.LocalTransform>(champEntity);

    // If no input, STOP by setting target to current position
    if(Mathf.Abs(forward) < 0.1f && Mathf.Abs(strafe) < 0.1f) {
      EntityManager.SetComponentData(champEntity, new ChampMoveTargetPosition {
        Value = currentTransform.Position
      });
      return;
    }

    // Calculate camera-relative movement direction
    Vector3 cameraForward = mainCamera.transform.forward;
    cameraForward.y = 0f;
    cameraForward.Normalize();

    Vector3 cameraRight = mainCamera.transform.right;
    cameraRight.y = 0f;
    cameraRight.Normalize();

    Vector3 moveDirection = (cameraForward * forward + cameraRight * strafe).normalized;

    // Set target position a short distance ahead in movement direction (tighter feel than 10 units)
    float lookAhead = 1.0f; // try 0.5f–2.0f to taste
    float3 targetPosition = currentTransform.Position + (float3)(moveDirection * lookAhead);

    // Update the move target component
    EntityManager.SetComponentData(champEntity, new ChampMoveTargetPosition {
      Value = targetPosition
    });

    // Clear any auto-attack target when moving (optional - comment out if you don't want this)
    if(SystemAPI.HasComponent<ChampTargetGhost>(champEntity)) {
      EntityManager.SetComponentData(champEntity, new ChampTargetGhost { TargetId = 0 });
    }
  }

  private void SetAutoAttackTargetEntity(Unity.Physics.RaycastHit closestHit) {
    Entity champEntity = SystemAPI.GetSingletonEntity<OwnerChampTag>();
    MobaTeam champTeam = SystemAPI.GetComponent<MobaTeam>(champEntity);
    Entity champTargetEntity = Entity.Null;

    foreach(var (xform, mobaTeam, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<MobaTeam>>()
        .WithEntityAccess()) {
      //TODO: Add component for adjusting hit distance precision on auto attack
      if(math.distance(xform.ValueRO.Position, closestHit.Position) < 1.25f
        && mobaTeam.ValueRO.Value != champTeam.Value
        ) {
        champTargetEntity = entity;
      }
    }

    //Remove the auto attack target
    //Entities created with new Entity() have a 0 value index
    //Therefore the check here indicates no target entity was found in the above idiomatic foreach
    //Additionally, the player clicked away from the existing target and wants to move elsewhere
    if(champTargetEntity == Entity.Null && SystemAPI.HasComponent<ChampTargetGhost>(champEntity)) {
      EntityManager.SetComponentData(champEntity, new ChampTargetGhost { TargetId = 0 });
    }
    else {
      //Debug.Log($"Setting targetEntity to {champTargetEntity.Index}");
      if(SystemAPI.HasComponent<GhostInstance>(champTargetEntity)) {
        var targetGhostId = SystemAPI.GetComponent<GhostInstance>(champTargetEntity);
        //Debug.Log($"RAYCAST HIT ON GHOST! {targetGhostId.ghostId} on entity {champTargetEntity.Index}");
        EntityManager.SetComponentData(champEntity, new ChampTargetGhost { TargetId = targetGhostId.ghostId });
      }
      else
        Debug.LogWarning("Got a raycast hit but the entity doesn't have a GhostId");
    }
  }

  private RaycastInput GetRayCastInput(ref CollisionWorld collisionWorld, LocalTransform champPosition) {
    //This get call looks for an Entity where EntityManager.AddComponent<T>(entity) call has been made
    //In this case that call occurs in MainCameraAuthoring Baker.
    Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTag>();
    Camera mainCamera = EntityManager.GetComponentObject<MainCamera>(cameraEntity).Value;

    //TODO: Add a component for controlling range (e.g. for spells and skillshots)
    var cameraDistFromChamp = math.distance(champPosition.Position, mainCamera.transform.position);
    float rayDist = 20f+cameraDistFromChamp;


    UnityEngine.Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    Vector3 rayEnd = ray.origin + ray.direction * rayDist;
    Debug.DrawRay(ray.origin, ray.direction * rayDist, Color.yellow, 2f);

    RaycastInput selectionInput = new RaycastInput {
      Start = ray.origin,
      End = rayEnd,
      Filter = _selectionFilter
    };

    return selectionInput;
  }
}

/*
  Mouse Position: float3(938f, 11f, 0f) - worldPosition: (50.00, 6.96, 43.42) - rayStart: (938.00, 11.00, 0.00)
  Mouse Position: float3(282f, 227f, 0f) - worldPosition: (50.00, 6.96, 43.42) - rayStart: (282.00, 227.00, 0.00)
  Mouse Position: float3(1213f, 373f, 0f) - worldPosition: (50.00, 6.96, 43.42) - rayStart: (1213.00, 373.00, 0.00)
  Mouse Position: float3(1029f, 101f, 0f) - worldPosition: (51.46, 10.63, 46.82) - rayStart: (1029.00, 101.00, 0.00)
  Mouse Position: float3(1141f, 221f, 0f) - worldPosition: (51.46, 10.63, 46.82) - rayStart: (1141.00, 221.00, 0.00)
*/