using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class CountdownToGameStartSystem : SystemBase {
  public Action<int> OnUpdateCountdownText;
  public Action OnCountdownEnd;

  protected override void OnCreate() {
    RequireForUpdate<NetworkTime>();
   }


  protected override void OnUpdate() {
    var networkTime = SystemAPI.GetSingleton<NetworkTime>();
    if(!networkTime.IsFirstTimeFullyPredictingTick) return;
    var currentTick = networkTime.ServerTick;

    var ecb = new EntityCommandBuffer(Allocator.Temp);

    foreach(var (gameStartTick, entity) in SystemAPI.Query<GameStartTick>().WithAll<Simulate>().WithEntityAccess()) {
      if(currentTick.Equals(gameStartTick.Value) || currentTick.IsNewerThan(gameStartTick.Value)) {
        var gamePlayingEntity = ecb.CreateEntity();
        ecb.SetName(gamePlayingEntity, "GamePlayingEntity");
        ecb.AddComponent<GamePlayingTag>(gamePlayingEntity);
        ecb.DestroyEntity(entity);
         OnCountdownEnd?.Invoke();

      }
      else {
        var ticksToStart = gameStartTick.Value.TickIndexForValidTick - currentTick.TickIndexForValidTick;
        var simTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
        var secondsToStart = (int)math.ceil((float)ticksToStart/simTickRate);
        OnUpdateCountdownText?.Invoke(secondsToStart);
      }
    }

    ecb.Playback(EntityManager);
    ecb.Dispose();
  }
}
