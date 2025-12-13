using NUnit.Framework;
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

    foreach(var (ability, targetEntity, transform, entity) in
        SystemAPI.Query<
          RefRO<AutoAttackMoveSpeed>,
          RefRO<AutoAttackTarget>,
          RefRW<LocalTransform>>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {

      if(!state.EntityManager.Exists(targetEntity.ValueRO.Target)) {
        Debug.LogError($"[{worldName}][Tick:{currentTick}] " +
          $"DESTROYING AutoAttack Entity:{entity.Index} - Target entity {targetEntity.ValueRO.Target.Index} does NOT exist!");
        ecb.AddComponent<DestroyEntityTag>(entity);

        if(targetEntity.ValueRO.Target.Index == 0) {
          Debug.LogError($"  -> Target has Index 0 (never set properly)");
        }

        continue;
      }

      if(SystemAPI.HasComponent<LocalTransform>(targetEntity.ValueRO.Target)) {
        var targetPos = SystemAPI.GetComponent<LocalTransform>(targetEntity.ValueRO.Target);
        float distanceToTarget = math.distance(transform.ValueRO.Position, targetPos.Position);

        if(currentTick.TickIndexForValidTick % 10 == 0) {
          Debug.Log($"[{worldName}][Tick:{currentTick}] AutoAttack Entity:{entity.Index} Distance to target: {distanceToTarget:F2}");
        }

        float3 moveTarget = targetPos.Position;
        moveTarget.y = transform.ValueRO.Position.y;

        float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
        var moveVector = moveDirection * ability.ValueRO.Value * SystemAPI.Time.DeltaTime;
        transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
        transform.ValueRW.Position += moveVector;
      }
      else {
        Debug.LogWarning($"[{worldName}][Tick:{currentTick}] Target entity {targetEntity.ValueRO.Target.Index} exists but has no LocalTransform!");
      }
      //ecb.Playback(state.EntityManager);
      //ecb.Dispose();
    }
  }
}