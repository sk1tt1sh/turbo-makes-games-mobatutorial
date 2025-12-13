using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ChampMoveSystem : ISystem {

  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    state.RequireForUpdate<GamePlayingTag>();
  }

  public void OnUpdate(ref SystemState state) {
    float deltaTime = SystemAPI.Time.DeltaTime;
    const float MOVEMENT_THRESHOLD = 0.5f; // Deadzone to prevent micro-jitter
    
    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

    ComponentLookup<GhostInstance> ghostIdLookup = SystemAPI.GetComponentLookup<GhostInstance>(true);
    NativeHashMap<int, Entity> ghostIdToEntityMap = new NativeHashMap<int, Entity>(500, Allocator.Temp);

    foreach(var (ghostId, entity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess()) {
      ghostIdToEntityMap.TryAdd(ghostId.ValueRO.ghostId, entity);
    }

    foreach(var (transform, movePosition, autoProps, moveSpeed, targetGhostId, entity) in
        SystemAPI.Query<
          RefRW<LocalTransform>,
          RefRO<ChampMoveTargetPosition>,
          RefRO<ChampAutoAttackProperties>,
          RefRO<CharacterMoveSpeed>,
          RefRO<ChampTargetGhost>>()
        .WithNone<ChampDashingTag>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {

      float3 moveTarget;

      //Check if the champ has an autoattack target and override the move position
      if(targetGhostId.ValueRO.TargetId != 0) {
        var target = SystemAPI.GetComponentRW<ChampTargetGhost>(entity);
        Debug.Log("Checkpoint 1 - Champ has target ghost");

        if(ghostIdToEntityMap.TryGetValue(targetGhostId.ValueRO.TargetId, out Entity targetEntity) 
          && SystemAPI.HasComponent<LocalTransform>(targetEntity)) 
        {
          LocalTransform targetPos = SystemAPI.GetComponent<LocalTransform>(targetEntity);
          moveTarget = targetPos.Position;
          moveTarget.y = transform.ValueRO.Position.y;

          if(math.distance(transform.ValueRO.Position, moveTarget) <= autoProps.ValueRO.Range) {
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

        // Stop if within threshold - prevents micro-jitter from floating point drift
        if(math.distance(transform.ValueRO.Position, moveTarget) < MOVEMENT_THRESHOLD) {
          continue;
        }
      }

      float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
      
      // Additional safety: don't move if direction is invalid or too small
      if(math.lengthsq(moveDirection) < 0.001f) {
        Debug.LogWarning("Hit additional move safety");
        continue;
      }

      var moveVector = moveDirection * moveSpeed.ValueRO.Value * deltaTime;
      transform.ValueRW.Position += moveVector;
      transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
    }
  }
}
