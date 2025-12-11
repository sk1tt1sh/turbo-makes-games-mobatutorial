using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct AutoAttackMoveSystem : ISystem {
  [BurstCompile]
  public void OnUpdate(ref SystemState state) {

    var ecb = new EntityCommandBuffer(Allocator.Temp);

    foreach(var (ability, targetEntity, transform, entity) in
        SystemAPI.Query<
          RefRO<AutoAttackMoveSpeed>,
          RefRO<AutoAttackTarget>,
          RefRW<LocalTransform>>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {

      if(!state.EntityManager.Exists(targetEntity.ValueRO.Target)) {
        Debug.LogError("No target entity for auto attack.");
        ecb.AddComponent<DestroyEntityTag>(entity);
        continue;
      }

      if(SystemAPI.HasComponent<LocalTransform>(targetEntity.ValueRO.Target)){
        var targetPos = SystemAPI.GetComponent<LocalTransform>(targetEntity.ValueRO.Target);
        float3 moveTarget = targetPos.Position;
        moveTarget.y = transform.ValueRO.Position.y;

        float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
        var moveVector = moveDirection * ability.ValueRO.Value * SystemAPI.Time.DeltaTime;
        transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
        transform.ValueRW.Position += moveVector;
      }
    }
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}