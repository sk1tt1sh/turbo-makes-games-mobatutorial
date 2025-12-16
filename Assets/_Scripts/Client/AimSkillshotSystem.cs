using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial struct AimSkillshotSystem : ISystem {
  private CollisionFilter selectionFilter;

  public void OnCreate(ref SystemState state) {
    selectionFilter = new CollisionFilter {
      BelongsTo = 1 << 5,
      CollidesWith = 1 << 0
    };
  }

  public void OnUpdate(ref SystemState state) {
    foreach(var (aim, transform, skillShotUiRef) in
        SystemAPI.Query<RefRW<AimInput>, LocalTransform, SkillShotUIReference>()
        .WithAny<AimSkillShotTag, AimChargeAbilityTag>()
        .WithAll<OwnerChampTag>()) {

      skillShotUiRef.Value.transform.position = transform.Position;
      CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
      Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTag>();
      Camera mainCamera = state.EntityManager.GetComponentObject<MainCamera>(cameraEntity).Value;

      Vector3 mousePosition = Input.mousePosition;
      mousePosition.z = 1000f;
      Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

      var selectionInput = new RaycastInput {
        Start = mainCamera.transform.position,
        End = worldPosition,
        Filter = selectionFilter
      };

      if(collisionWorld.CastRay(selectionInput, out var hit)) {
        var directionToTarget = hit.Position - transform.Position;
        directionToTarget.y = transform.Position.y;
        directionToTarget = math.normalize(directionToTarget);
        aim.ValueRW.Value = directionToTarget;

        var angleRg = math.atan2(directionToTarget.z, directionToTarget.x);
        var angleDeg = math.degrees(angleRg);
        skillShotUiRef.Value.transform.rotation = Quaternion.Euler(0f, -angleDeg, 0f);

      }
    }
  }
}