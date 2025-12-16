using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct BeginSkillshotSystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<NetworkTime>();
  }

  public void OnUpdate(ref SystemState state) {
    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
    NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
    bool isServer = state.WorldUnmanaged.IsServer();

    if(!netTime.IsFirstTimeFullyPredictingTick) return;
    NetworkTick currentTick = netTime.ServerTick;

    foreach(var (abilityInput, abilityPrefab, mobaTeam, localXform, abilityCdTicks, abilityCdTargetTicks, entity) in
        SystemAPI.Query<RefRO<AbilityInput>, RefRO<AbilityPrefabs>, RefRO<MobaTeam>,
        RefRO<LocalTransform>, RefRO<AbilityCooldownTicks>, DynamicBuffer<AbilityCooldownTargetTicks>>()
        .WithAll<Simulate>().WithNone<AimSkillShotTag>().WithEntityAccess()) {

      bool isOnCoolDown = AbilityCooldownCheck
        .IsOnCooldown(ref netTime, currentTick, abilityCdTargetTicks, AbilitiesList.SkillShot);

      if(isOnCoolDown) continue;
      if(!abilityInput.ValueRO.SkillShotAbility.IsSet) continue;

      ecb.AddComponent<AimSkillShotTag>(entity);

      if(isServer || !SystemAPI.HasComponent<OwnerChampTag>(entity)) continue;
      var skillShotUIPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().SkillShot;
      var skillShotUIRef = Object.Instantiate(skillShotUIPrefab, localXform.ValueRO.Position, quaternion.identity);
      ecb.AddComponent(entity, new SkillShotUIReference { Value = skillShotUIRef });
    }

    foreach(var (abilityInput, abilityPrefab, mobaTeam, localXform, abilityCdTicks, abilityCdTargetTicks, aimInput, entity) in
        SystemAPI.Query<RefRO<AbilityInput>, RefRO<AbilityPrefabs>, RefRO<MobaTeam>,
        RefRO<LocalTransform>,
        RefRO<AbilityCooldownTicks>,
        DynamicBuffer<AbilityCooldownTargetTicks>,
        RefRO<AimInput>>()
        .WithAll<Simulate, AimSkillShotTag>().WithEntityAccess()) {

      if(!abilityInput.ValueRO.ConfirmSkillShotAbility.IsSet) continue;

      Entity skillShotAbility = ecb.Instantiate(abilityPrefab.ValueRO.SkillShotAbility);

      LocalTransform newPosition = LocalTransform.FromPositionRotation(
        localXform.ValueRO.Position, quaternion.LookRotationSafe(aimInput.ValueRO.Value, math.up()));

      ecb.SetComponent(skillShotAbility, newPosition);
      ecb.SetComponent(skillShotAbility, mobaTeam.ValueRO);
      ecb.RemoveComponent<AimSkillShotTag>(entity);//Removes the Aim tag from the champion entity

      if(isServer) continue;
      abilityCdTargetTicks.GetDataAtTick(currentTick, out var curTargetTick);

      var newCdTargetTick = currentTick;
      newCdTargetTick.Add(abilityCdTicks.ValueRO.SkillShotAbility);
      curTargetTick.SkillShotAbility = newCdTargetTick;

      var nextTick = currentTick;
      nextTick.Add(1u);
      curTargetTick.Tick = nextTick;

      abilityCdTargetTicks.AddCommandData(curTargetTick);
    }

    foreach(var (abilityInput, skillShotUiRef, entity) in
        SystemAPI.Query<AbilityInput, SkillShotUIReference>()
        .WithAll<Simulate>().WithEntityAccess()) {

      if(!abilityInput.ConfirmSkillShotAbility.IsSet) continue;
      Object.Destroy(skillShotUiRef.Value);
      ecb.RemoveComponent<SkillShotUIReference>(entity);
    }

    //If player died or cancelled skillshot, remove UI
    foreach(var (skillshotUiRef, entity) in
        SystemAPI.Query<SkillShotUIReference>()
        .WithAll<Simulate>().WithNone<LocalTransform>().WithEntityAccess()) {

      Object.Destroy(skillshotUiRef.Value);
      ecb.RemoveComponent<SkillShotUIReference>(entity);
    }

    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}