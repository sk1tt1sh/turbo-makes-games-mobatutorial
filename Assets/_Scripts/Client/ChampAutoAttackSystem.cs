using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateBefore(typeof(ChampMoveSystem))]
partial struct ChampAutoAttackSystem : ISystem {
  [BurstCompile]
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<GamePlayingTag>();
    state.RequireForUpdate<NetworkTime>();
  }

  //This might need to be a job...
  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
    NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
    
    //This query should locate all champions with target entities
    //Then if the entity value != 0 or null generate the respective
    //Autoattack prefab
    //The prefab destination will need to be set to match the entity
    //A projectile system (
    foreach(var (xForm, autoProps, target, aaCooldown, mobaTeam, entity) in 
      SystemAPI.Query<
        RefRO<LocalTransform>,
        RefRO<ChampAutoAttackProperties>,
        RefRO<ChampTargetEntity>,
        DynamicBuffer<AutoAttackCooldown>,
        RefRO<MobaTeam>>()
      .WithAll<Simulate>()
      .WithEntityAccess()) 
    {
      if(target.ValueRO.Target == Entity.Null || target.ValueRO.Target.Index == 0) continue;
      if(!SystemAPI.HasComponent<LocalTransform>(target.ValueRO.Target)) continue; 
      
      if(!aaCooldown.GetDataAtTick(currentTick, out var cdExpiringTick)) {
        cdExpiringTick.Value = NetworkTick.Invalid;
      }

      var targetPosition = SystemAPI.GetComponentRO<LocalTransform>(target.ValueRO.Target);
      if(math.distance(targetPosition.ValueRO.Position, xForm.ValueRO.Position) > autoProps.ValueRO.Range) continue;

      bool offCoolDown = !cdExpiringTick.Value.IsValid || currentTick.IsNewerThan(cdExpiringTick.Value);
      if(!offCoolDown) continue;

      
      if(!SystemAPI.HasComponent<LocalTransform>(target.ValueRO.Target)) {
        Debug.LogWarning("Autoattack target transform is missing");
        continue;
      }

      Entity autoAttackEntity = ecb.Instantiate(autoProps.ValueRO.AttackPrefab);

      float3 directionToTarget = math.normalize(targetPosition.ValueRO.Position - xForm.ValueRO.Position);
      float3 spawnPos = xForm.ValueRO.Position + directionToTarget * math.length(autoProps.ValueRO.FirePointOffset);
      LocalTransform spawnPosition = LocalTransform.FromPositionRotation(spawnPos,
        quaternion.LookRotationSafe(directionToTarget, math.up()));

      //Transfer the target entity to the autoattack
      ecb.AddComponent(autoAttackEntity, new AutoAttackTarget { Target = target.ValueRO.Target });
      ecb.SetComponent(autoAttackEntity, spawnPosition);
      ecb.SetComponent(autoAttackEntity, mobaTeam.ValueRO);
      
      if(state.WorldUnmanaged.IsServer()) continue;

      NetworkTick newCooldown = currentTick;
      newCooldown.Add(autoProps.ValueRO.CooldownTicks);
      aaCooldown.AddCommandData(new AutoAttackCooldown { Tick = currentTick, Value = newCooldown });
    }
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}