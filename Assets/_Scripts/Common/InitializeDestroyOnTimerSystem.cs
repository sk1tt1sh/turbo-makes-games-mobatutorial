using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public partial struct InitializeDestroyOnTimerSystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<NetworkTime>();
  }


  public void OnUpdate(ref SystemState state) {
    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
    int simTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
    var currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;


    foreach(var(destroyOnTimer, entity) in 
        SystemAPI.Query<DestroyOnTimer>().WithNone<DestroyAtTick>()
        .WithEntityAccess()) {

      uint lifetimeInTicks = (uint)math.round(destroyOnTimer.Value * simTickRate);
      NetworkTick targetTick = currentTick;
      targetTick.Add(lifetimeInTicks);
      ecb.AddComponent(entity, new DestroyAtTick { Value = targetTick });
    }
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}
