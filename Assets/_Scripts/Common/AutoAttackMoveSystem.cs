using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(NpcAttackSystem))]
public partial struct AutoAttackMoveSystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    state.RequireForUpdate<NetworkTime>();
    state.RequireForUpdate<GamePlayingTag>();
  }

  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    var networkTime = SystemAPI.GetSingleton<NetworkTime>();
    var currentTick = networkTime.ServerTick;
    var worldName = state.World.Name;
    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

    ComponentLookup<GhostInstance> ghostIdLookup = SystemAPI.GetComponentLookup<GhostInstance>(true);
    NativeHashMap<int, Entity> ghostIdToEntityMap = new NativeHashMap<int, Entity>(500, Allocator.Temp);

    foreach(var (ghostId, entity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess()) {
      ghostIdToEntityMap.TryAdd(ghostId.ValueRO.ghostId, entity);
    }

    foreach(var (ability, targetGhostId, transform, entity) in
        SystemAPI.Query<
          RefRO<AutoAttackMoveSpeed>,
          RefRO<AutoAttackTarget>,
          RefRW<LocalTransform>>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {


      if(!ghostIdToEntityMap.TryGetValue(targetGhostId.ValueRO.Value, out Entity targetEntity)) {
        ecb.AddComponent<DestroyEntityTag>(entity);
      }
      //if(!state.EntityManager.Exists(targetEntity.ValueRO.Target)) {
      //  ecb.AddComponent<DestroyEntityTag>(entity);
      //  continue;
      //}

      if(SystemAPI.HasComponent<LocalTransform>(targetEntity)) {
        var targetPos = SystemAPI.GetComponent<LocalTransform>(targetEntity);
        float distanceToTarget = math.distance(transform.ValueRO.Position, targetPos.Position);

        float3 moveTarget = targetPos.Position;
        moveTarget.y = transform.ValueRO.Position.y;

        float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
        var moveVector = moveDirection * ability.ValueRO.Value * SystemAPI.Time.DeltaTime;
        transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
        transform.ValueRW.Position += moveVector;
      }
      else {
        Debug.LogWarning($"[{worldName}][Tick:{currentTick}] Target entity {targetEntity.Index} exists but has no LocalTransform!");
      }
    }
  }
}