using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ChampMoveSystem : ISystem {

  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<GamePlayingTag>();
  }

  public void OnUpdate(ref SystemState state) {
    float deltaTime = SystemAPI.Time.DeltaTime;

    foreach(var (transform, movePosition, autoProps, moveSpeed, entity) in
        SystemAPI.Query<
          RefRW<LocalTransform>,
          RefRO<ChampMoveTargetPosition>,
          RefRO<ChampAutoAttackProperties>,
          RefRO<CharacterMoveSpeed>
          >()
        .WithNone<ChampDashingTag>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {

      float3 moveTarget;

      //Check if the champ has an autoattack target and override the move position
      if(SystemAPI.HasComponent<ChampTargetEntity>(entity)) {
        var target = SystemAPI.GetComponentRW<ChampTargetEntity>(entity);
        var hasLocalTransform = SystemAPI.HasComponent<LocalTransform>(target.ValueRO.Target);

        if(hasLocalTransform) {
          var targetPos = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.Target);
          moveTarget = targetPos.Position;
          moveTarget.y = transform.ValueRO.Position.y;

          //TODO: Add a component to champ for their auto attack range
          if(math.distance(transform.ValueRO.Position, moveTarget) <= autoProps.ValueRO.Range) {
            //Debug.Log($"Within auto-range not moving - {math.distance(transform.ValueRO.Position, moveTarget)}");
            continue;
          }
        }
        else {
          moveTarget = movePosition.ValueRO.Value;
          moveTarget.y = transform.ValueRO.Position.y;
          //Proably need to remove that target entity. The champion will probably move to last target position
          //We might be able to use a hack on the component data and set the position as the target moves
        }
      }
      else {
        moveTarget = movePosition.ValueRO.Value;
        moveTarget.y = transform.ValueRO.Position.y;

        if(math.distance(transform.ValueRO.Position, moveTarget) < 1f) {
          continue;
        }
      }

      float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
      var moveVector = moveDirection * moveSpeed.ValueRO.Value * deltaTime;
      transform.ValueRW.Position += moveVector;
      transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
    }
  }
}