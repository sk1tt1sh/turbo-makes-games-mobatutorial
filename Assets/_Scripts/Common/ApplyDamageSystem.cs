using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameDamageSystem))]
public partial struct ApplyDamageSystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<GamePlayingTag>();
    state.RequireForUpdate<NetworkTime>();
  }

  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    var currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
    var ecb = new EntityCommandBuffer(Allocator.Temp);

    foreach(var (currHitpoints, damageThisTickBuff, entity) in
        SystemAPI.Query<RefRW<CurrentHitPoints>, DynamicBuffer<DamageThisTick>>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {

      //These 2 lines ensure were using the current tick not a previous tick
      if(!damageThisTickBuff.GetDataAtTick(currentTick, out var damageThisTick)) continue;
      if(damageThisTick.Tick != currentTick) continue;
      if(damageThisTick.Value <= 0) continue;

      currHitpoints.ValueRW.Value -= damageThisTick.Value;
      if(currHitpoints.ValueRO.Value <= 0) {
        //Tag entity for destroying
        ecb.AddComponent<DestroyEntityTag>(entity);
      }
    }
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}