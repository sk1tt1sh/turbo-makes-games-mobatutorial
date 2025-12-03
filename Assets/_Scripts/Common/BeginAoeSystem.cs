using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct BeginAoeSystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<NetworkStreamInGame>();
    state.RequireForUpdate<NetworkTime>();
    state.RequireForUpdate<GhostOwner>();
  }

  /// <summary>
  /// We use this because we want the spawn to occur towards the beginning of the frame
  /// Otherwise it could spawn at a different location for a frame
  /// </summary>
  /// <param name="state"></param>
  //[BurstCompile]
  public void OnUpdate(ref SystemState state) {
    var netTimeSingleton = SystemAPI.GetSingleton<NetworkTime>();
    var currentTick = netTimeSingleton.ServerTick;
    
    if(!netTimeSingleton.IsFirstTimeFullyPredictingTick) return;

    //Built-in Entity Command buffer.
    var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
    /* World is managed so it can’t be used inside burst hence as a rule of thumb, 
     * you just use World when your in managed land and Unmanaged world when 
     * you’re in a bursted ISystem (but you get it from state not from World)
     */



    foreach(var (aoeInput, aoeTeam, aoeTransfrom, owner, cooldownTicks,abilityCooldownTargetTick, prefab) in
      SystemAPI.Query<RefRO<AbilityInput>, RefRO<MobaTeam>, 
        RefRO<LocalTransform>, RefRO<GhostOwner>, RefRO<AbilityCooldownTicks>,
        DynamicBuffer<AbilityCooldownTargetTicks>, RefRO<AbilityPrefabs>>()
        .WithAll<Simulate>()) {

      // If the server simulation is running slow it skips over networkticks
      // if the cooldown was  on the skipped tick it could lock cooldown forever
      // simulationstepbatch size is how many ticks are being simulated
      var isOnCooldown = AbilityCooldownCheck
        .IsOnCooldown(ref netTimeSingleton, currentTick, abilityCooldownTargetTick, AbilitiesList.Aoe);

      if(isOnCooldown) continue;

      if(aoeInput.ValueRO.AoeAbility.IsSet) {
        var curTargetTicks = new AbilityCooldownTargetTicks();
        Entity newAoeAbility = ecb.Instantiate(prefab.ValueRO.AoeAbility);
        LocalTransform abilityXform = LocalTransform.FromPosition(aoeTransfrom.ValueRO.Position);
        //Adds the location data to the new entity
        ecb.SetComponent(newAoeAbility, abilityXform);
        //Associates the team
        ecb.SetComponent(newAoeAbility, new MobaTeam { Value = aoeTeam.ValueRO.Value });

        // Set spawn data for predicted spawn matching
        ecb.AddComponent(newAoeAbility, new PredictedSpawnData {
          SpawnTick = currentTick,
          OwnerNetworkId = owner.ValueRO.NetworkId
        });

        if(state.WorldUnmanaged.IsServer()) continue;

        //var spawnListEntity = SystemAPI.GetSingletonEntity<PredictedGhostSpawnList>();
        //ecb.AppendToBuffer(spawnListEntity, new PredictedGhostSpawn { entity = newAoeAbility });

        //Command data is set client side only
        //We get the next tick because the command data will only spawn locally
        //and the server will ignore it. Therefore the simulated next tick will have
        //the command data
        var newCooldownTargetTick = currentTick;
        newCooldownTargetTick.Add(cooldownTicks.ValueRO.AoeAbility);
        curTargetTicks.AoeAbility = newCooldownTargetTick;
        var nextTick = currentTick;
        nextTick.Add(1u);
        curTargetTicks.Tick = nextTick;
        abilityCooldownTargetTick.AddCommandData(curTargetTicks);


      }
    }
    //ecb.Playback(state.EntityManager);
  }
}