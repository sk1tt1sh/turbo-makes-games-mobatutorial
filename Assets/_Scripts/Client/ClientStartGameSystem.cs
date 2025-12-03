using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ClientStartGameSystem : SystemBase {
  public Action<int> OnUpdatePlayersRemainingToStart;
  public Action OnGameStartCountdown;

  protected override void OnCreate() {
     base.OnCreate();
  }

  protected override void OnUpdate() {
    
    var ecb = new EntityCommandBuffer(Allocator.Temp);
    foreach(var (playersRemaining,entity) in 
        SystemAPI.Query<PlayersRemainingToStart>()
        .WithAll<ReceiveRpcCommandRequest>()
        .WithEntityAccess()) { 

      //Destroy the RPC entity so it doesn't get reused
      ecb.DestroyEntity(entity);
      OnUpdatePlayersRemainingToStart?.Invoke(playersRemaining.Value);
    }

    foreach(var (gameStartTickRpc, entity) in 
        SystemAPI.Query<GameStartTickRpc>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {

      ecb.DestroyEntity(entity);
      OnGameStartCountdown?.Invoke();

      //Note this will only exist on clientside.
      //See serverprocessgameentryrequest system where it's set there so the server knows
      var gameStartEntity = ecb.CreateEntity();
      ecb.AddComponent(gameStartEntity, new GameStartTick { Value = gameStartTickRpc.Value });
    }

    ecb.Playback(EntityManager);
    ecb.Dispose();
  }
}
