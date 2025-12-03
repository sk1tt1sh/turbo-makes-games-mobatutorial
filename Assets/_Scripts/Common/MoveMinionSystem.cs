using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct MoveMinionSystem : ISystem {
  [BurstCompile]
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<GamePlayingTag>();
  }

  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    var deltaTime = SystemAPI.Time.DeltaTime;
    foreach(var (transform, pathPositions, pathIndex, moveSpeed, targetEntity)
        in SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<MinionPathPosition>,
        RefRW<MinionPathIndex>, CharacterMoveSpeed, RefRO<NpcTargetEntity>>()
        .WithAll<Simulate>()) {

      if(pathPositions.Length == 0) {
        Debug.LogWarning($"pathPositions length 0. Access position: [{pathIndex.ValueRO.Value}]");
        continue;
      }

      var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
      if(transformLookup.HasComponent(targetEntity.ValueRO.Value)) {
        //Effectively stops the minion from moving if it sees a target
        continue;
      }

      var currTargetPos = pathPositions[pathIndex.ValueRO.Value].Value;
      if(math.distance(currTargetPos, transform.ValueRO.Position) <= 1.5) {
        //Stops it from moving 
        if(pathIndex.ValueRO.Value >= pathPositions.Length - 1) continue;
        pathIndex.ValueRW.Value++;
        currTargetPos = pathPositions[pathIndex.ValueRO.Value].Value;
      }
      currTargetPos.y = transform.ValueRO.Position.y;
      var curHeading = math.normalizesafe(currTargetPos - transform.ValueRO.Position);
      transform.ValueRW.Position += curHeading * moveSpeed.Value * deltaTime;
      transform.ValueRW.Rotation = quaternion.LookRotationSafe(curHeading, math.up());
    }
  }

  [BurstCompile]
  public void OnDestroy(ref SystemState state) {

  }
}