using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct BeginChargeAbilitySystem : ISystem {
  [BurstCompile]
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<NetworkTime>();
  }

  public void OnUpdate(ref SystemState state) {
    NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
    if(!netTime.IsFirstTimeFullyPredictingTick) return;

    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
    bool isServer = state.WorldUnmanaged.IsServer();

    NetworkTick currentTick = netTime.ServerTick;
    var transforms = SystemAPI.GetComponentLookup<LocalTransform>(false);

    //Add the aim indicator for the attack
    foreach(var (abilityInput, abilityCDTarget, entity) in
        SystemAPI.Query<RefRO<AbilityInput>, DynamicBuffer<AbilityCooldownTargetTicks>>()
        .WithAll<Simulate>()
        .WithNone<AimChargeAbilityTag, AimSkillShotTag>()
        .WithEntityAccess()) {

      if(transforms.HasComponent(entity) == false) {
        Debug.LogWarning("Tranform for enity was not found when adding aim indicator for Charge");
        continue;
      }

      bool isOnCD = AbilityCooldownCheck
        .IsOnCooldown(ref netTime, currentTick, abilityCDTarget, AbilitiesList.Charge);
      if(isOnCD) continue;
      if(!abilityInput.ValueRO.ChargeAttack.IsSet) continue;

      ecb.AddComponent<AimChargeAbilityTag>(entity);
      if(isServer || !SystemAPI.HasComponent<OwnerChampTag>(entity)) continue;
      var aimChargePrefb = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().ChargeAttackAim;
      var aimChargeUIRef = Object.Instantiate(aimChargePrefb, transforms[entity].Position, quaternion.identity);
      ecb.AddComponent(entity, new SkillShotUIReference { Value = aimChargeUIRef });
    }

    //Remove the ChargeTarget tag and set the cooldown
    foreach(var (xForm, abilityInput, abilityCDTargetTicks, abilityPrefabs, abilityCDTicks, aim, moveData, entity) in
        SystemAPI.Query<
          RefRW<LocalTransform>,
          RefRO<AbilityInput>,
          DynamicBuffer<AbilityCooldownTargetTicks>,
          RefRO<AbilityPrefabs>,
          RefRO<AbilityCooldownTicks>,
          RefRO<AimInput>,
          RefRO<CharacterMoveSpeed>>()
        .WithAll<Simulate, AimChargeAbilityTag>()
        .WithNone<ChampDashingTag>()
        .WithEntityAccess()) {

      //Prevent from going on cooldown
      if(!abilityInput.ValueRO.ConfirmChargeAttack.IsSet) continue;
      if(!state.EntityManager.HasComponent<MobaTeam>(entity)) continue;
      if(!state.EntityManager.HasComponent<ChampMoveTargetPosition>(entity)) continue;

      MobaTeam mobaTeam = state.EntityManager.GetComponentData<MobaTeam>(entity);
      ChampMoveTargetPosition moveTargetPosition = state.EntityManager.GetComponentData<ChampMoveTargetPosition>(entity);
      //var champXform = transforms[entity];
      xForm.ValueRW.Rotation = xForm.ValueRO.Rotation;

      //This will block the champ move when dashing
      ecb.AddComponent<ChampDashingTag>(entity);

      var chargeEffect = ecb.Instantiate(abilityPrefabs.ValueRO.ChargeAttackAbility);
      var chargeXform = LocalTransform.FromPositionRotation(xForm.ValueRO.Position,
        quaternion.LookRotationSafe(aim.ValueRO.Value, math.up()));
      chargeXform.Scale = 0.25f;

      ecb.SetComponent(chargeEffect, chargeXform);
      ecb.SetComponent(chargeEffect, mobaTeam);
      ecb.AddComponent(chargeEffect, new ChargeAbilityOwner { Owner = entity });
      ecb.AddComponent(chargeEffect, new ChampMoveTargetPosition { Value = moveTargetPosition.Value });
      ecb.AddComponent(chargeEffect, new CharacterMoveSpeed { DashSpeed = moveData.ValueRO.DashSpeed });
      ecb.AddComponent(chargeEffect, new ChampDashData { DistanceRemaining = moveData.ValueRO.DashDistance });
      ecb.AddComponent(entity, new ChampDashData { DistanceRemaining = moveData.ValueRO.DashDistance });
      ecb.RemoveComponent<AimChargeAbilityTag>(entity);

      if(isServer) continue;

      var dashDirection = xForm.ValueRO.Forward();
      dashDirection.y = 0;
      dashDirection = math.normalize(dashDirection);
      var dashEndPosition = xForm.ValueRO.Position + (dashDirection * moveData.ValueRO.DashDistance);
      ecb.SetComponent(entity, new ChampMoveTargetPosition { Value = dashEndPosition });

      //This should have cooldownstates for all abilities in it. 
      abilityCDTargetTicks.GetDataAtTick(currentTick, out var curTargetTick);

      var newCDTargetTick = currentTick;
      newCDTargetTick.Add(abilityCDTicks.ValueRO.ChargeAbility);
      curTargetTick.ChargeAbility = newCDTargetTick;

      var nextTick = currentTick;
      nextTick.Add(2u);
      curTargetTick.Tick = nextTick;

      abilityCDTargetTicks.AddCommandData(curTargetTick);
    }


    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}