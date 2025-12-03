using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class ChampMoveInputSystem : SystemBase {
  private MobaInputActions _inputActions;
  private CollisionFilter _selectionFilter;

  protected override void OnCreate() {
    _inputActions = new MobaInputActions();
    //This maps to the physics category names
    _selectionFilter = new CollisionFilter {
      BelongsTo = 1 << 5, //Raycasts group 
      CollidesWith = 1 << 0 //GroundPlane
    };
    RequireForUpdate<OwnerChampTag>();
  }

  protected override void OnStartRunning() {
    _inputActions.Enable();
    _inputActions.GameplayMap.SelectMovePosition.performed += OnSelectMovePosition;
    _inputActions.GameplayMap.ConfirmSkillShotAbility.performed += OnSkillShotConfirm;
  }

  protected override void OnStopRunning() {
    _inputActions.GameplayMap.SelectMovePosition.performed -= OnSelectMovePosition;
    _inputActions.Disable();
  }

  protected override void OnUpdate() {
  }

  private void OnSkillShotConfirm(InputAction.CallbackContext obj) {
    CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
    RaycastInput selectionInput = GetRayCastData(ref collisionWorld);

    if(collisionWorld.CastRay(selectionInput, out Unity.Physics.RaycastHit closestHit)) {
      Entity champEntity = SystemAPI.GetSingletonEntity<OwnerChampTag>();
      var entityTransform = EntityManager.GetComponentData<LocalTransform>(champEntity);
      if(!EntityManager.HasComponent<AimChargeAbilityTag>(champEntity)) {
        Debug.Log("Champ does not have AimChargeAbilityTage");
        return;
      }
      if(!EntityManager.HasComponent<CharacterMoveSpeed>(champEntity)) {
        Debug.Log("Champ does not have CharacterMoveSpeed component");
        return;
      }

      var dashInfo = EntityManager.GetComponentData<CharacterMoveSpeed>(champEntity);
      float3 offset = closestHit.Position - entityTransform.Position;
      float distance = math.length(offset);
      var targetPosition = entityTransform.Position + math.normalize(offset) * dashInfo.DashDistance;
      targetPosition.y = entityTransform.Position.y;
      Debug.Log($"Closest hit = {closestHit.Position}, current position {entityTransform.Position} - Distance {math.distance(closestHit.Position,entityTransform.Position)} - Clamped distance: {targetPosition}");
      EntityManager.SetComponentData(champEntity, new ChampMoveTargetPosition {
        Value = targetPosition
      });
    }
  }

  private void OnSelectMovePosition(InputAction.CallbackContext obj) {
    //Debug.Log("OnSelectMovePosition");
    CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
    RaycastInput selectionInput = GetRayCastData(ref collisionWorld);

    if(collisionWorld.CastRay(selectionInput, out Unity.Physics.RaycastHit closestHit)) {
      Entity champEntity = SystemAPI.GetSingletonEntity<OwnerChampTag>();
      EntityManager.SetComponentData(champEntity, new ChampMoveTargetPosition {
        Value = closestHit.Position
      });

    }
  }

  private RaycastInput GetRayCastData(ref CollisionWorld collisionWorld) {
    //This get call looks for an Entity where EntityManager.AddComponent<T>(entity) call has been made
    //In this case that call occurs in MainCameraAuthoring Baker.
    Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTag>();
    Camera mainCamera = EntityManager.GetComponentObject<MainCamera>(cameraEntity).Value;

    float3 mousePosition = Input.mousePosition;
    mousePosition.z = 100f;
    Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

    RaycastInput selectionInput = new RaycastInput {
      Start = mainCamera.transform.position,
      End = worldPosition,
      Filter = _selectionFilter
    };

    return selectionInput;
  }
}
