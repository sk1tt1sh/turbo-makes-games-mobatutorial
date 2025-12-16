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
    var worldName = state.World.Name;

    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
    NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();

    if(!netTime.IsFirstTimeFullyPredictingTick) return;

    NetworkTick currentTick = netTime.ServerTick;

    ComponentLookup<GhostInstance> networkIdLookup = SystemAPI.GetComponentLookup<GhostInstance>(true);
    NativeHashMap<int, Entity> networkIdToEntityMap = new NativeHashMap<int, Entity>(500, Allocator.Temp);

    foreach(var (ghostId, entity) in SystemAPI.Query<RefRO<GhostInstance>>().WithEntityAccess()) {
      networkIdToEntityMap.TryAdd(ghostId.ValueRO.ghostId, entity);
    }

    //This query should locate all champions with target entities
    //Then if the entity value != 0 or null generate the respective
    //Autoattack prefab
    //The prefab destination will need to be set to match the entity
    //A projectile system (
    foreach(var (xForm, autoProps, target, aaCooldown, mobaTeam, entity) in
      SystemAPI.Query<
        RefRO<LocalTransform>,
        RefRO<ChampAutoAttackProperties>,
        RefRO<ChampTargetGhost>,
        DynamicBuffer<AutoAttackCooldown>,
        RefRO<MobaTeam>>()
      .WithAll<Simulate>()
      .WithEntityAccess()) {

      if(target.ValueRO.TargetId == 0) {
        continue;
      }

      if(!networkIdToEntityMap.TryGetValue(target.ValueRO.TargetId, out Entity champTargetEnt)) {
        continue;
      }

      if(!SystemAPI.HasComponent<LocalTransform>(champTargetEnt)) {
        Debug.LogWarning($"[{state.World.Name}][Tick:{currentTick}] " +
          $"Champ Entity:{entity.Index} target {champTargetEnt.Index} has no LocalTransform - skipping spawn");
        continue;
      }

      if(!aaCooldown.GetDataAtTick(currentTick, out var cdExpiringTick)) {
        cdExpiringTick.Value = NetworkTick.Invalid;
      }

      var targetPosition = SystemAPI.GetComponentRO<LocalTransform>(champTargetEnt);
      if(math.distance(targetPosition.ValueRO.Position, xForm.ValueRO.Position) > autoProps.ValueRO.Range) {
        continue;
      }

      bool offCoolDown = !cdExpiringTick.Value.IsValid || currentTick.IsNewerThan(cdExpiringTick.Value);
      if(!offCoolDown) {
        continue;
      }

      if(!SystemAPI.HasComponent<LocalTransform>(champTargetEnt)) {
        continue;
      }

      Entity autoAttackEntity = ecb.Instantiate(autoProps.ValueRO.AttackPrefab);

      float3 directionToTarget = math.normalize(targetPosition.ValueRO.Position - xForm.ValueRO.Position);
      float3 spawnPos = xForm.ValueRO.Position + directionToTarget * autoProps.ValueRO.FirePointOffset;
      LocalTransform spawnPosition = LocalTransform.FromPositionRotation(spawnPos,
        quaternion.LookRotationSafe(directionToTarget, math.up()));

      float spawnDistance = math.distance(spawnPos, xForm.ValueRO.Position);

      //Transfer the target entity to the autoattack
      ecb.AddComponent(autoAttackEntity, new AutoAttackTarget { Value = target.ValueRO.TargetId });
      ecb.SetComponent(autoAttackEntity, spawnPosition);
      ecb.SetComponent(autoAttackEntity, mobaTeam.ValueRO);
      if(state.WorldUnmanaged.IsServer()) {
        continue;
      }

      NetworkTick newCooldown = currentTick;
      newCooldown.Add(autoProps.ValueRO.CooldownTicks);
      cdExpiringTick.Value = newCooldown;

      NetworkTick nextTick = currentTick;
      nextTick.Add(1u);
      cdExpiringTick.Tick = nextTick;

      aaCooldown.AddCommandData(cdExpiringTick);
    }


    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}