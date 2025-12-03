using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct DestroyEntitySystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    state.RequireForUpdate<NetworkTime>();
  }

  public void OnUpdate(ref SystemState state) {
    var netTime = SystemAPI.GetSingleton<NetworkTime>();
    
    if(!netTime.IsFirstTimeFullyPredictingTick) return;
    
    var currTick = netTime.ServerTick;

    var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
    foreach(var (transform, entity) in 
        SystemAPI.Query<RefRW<LocalTransform>>().WithAll<DestroyEntityTag, Simulate>()
        .WithEntityAccess()) {
      //This is a bit of a weird hack where the entity is destroyed on the server
      //But the client hides it until the server says "hey it's destroyed" 
      //Need to learn more about this...
      if(state.World.IsServer()) {
        ecb.DestroyEntity(entity);
      }
      //else if(state.World.IsClient()) {
      //  transform.ValueRW.Position = new Unity.Mathematics.float3(0, -1000,0);
      //}
    }
  }
}
