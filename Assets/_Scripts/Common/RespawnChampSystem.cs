using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class RespawnChampSystem : SystemBase {
  public Action<int> OnUpdateRespawnCountdown;
  public Action OnRespawn;

  protected override void OnCreate() {
    RequireForUpdate<NetworkTime>();
    RequireForUpdate<MobaPrefabs>();
  }

  protected override void OnUpdate() {
    var netTime = SystemAPI.GetSingleton<NetworkTime>();
    if(!netTime.IsFirstTimeFullyPredictingTick) return;
    var currTick = netTime.ServerTick;

    var isServer = World.IsServer();

    var ecb = new EntityCommandBuffer(Allocator.Temp);

    foreach(var respawnBuffer in SystemAPI.Query<DynamicBuffer<RespawnBufferElement>>().WithAll<RespawnTickCount, Simulate>()) {
      var respawnsToCleanup = new NativeList<int>(Allocator.Temp);
      for(int i = 0; i < respawnBuffer.Length; i++) {
        var curRespawn = respawnBuffer[i];

        if(currTick.Equals(curRespawn.RespawnTick) || currTick.IsNewerThan(curRespawn.RespawnTick)) {
          //Time to respawn it!
          if(isServer) {
            var networkId = SystemAPI.GetComponent<NetworkId>(curRespawn.NetworkEntity).Value;
            var playerSpawnInfo = SystemAPI.GetComponent<PlayerSpawnInfo>(curRespawn.NetworkEntity);

            var champPrefab = SystemAPI.GetSingleton<MobaPrefabs>().Champion;
            var newChamp = ecb.Instantiate(champPrefab);

            ecb.SetComponent(newChamp, new GhostOwner { NetworkId = networkId });
            ecb.SetComponent(newChamp, new MobaTeam { Value = playerSpawnInfo.Team });
            ecb.SetComponent(newChamp, new ChampTargetGhost { TargetId = 0 });
            ecb.SetComponent(newChamp, new ChampMoveTargetPosition { Value = playerSpawnInfo.SpawnPosition });
            ecb.SetComponent(newChamp, new NetworkEntityReference { Value = curRespawn.NetworkEntity });
            ecb.SetComponent(newChamp, LocalTransform.FromPosition(playerSpawnInfo.SpawnPosition));
            ecb.AppendToBuffer(curRespawn.NetworkEntity, new LinkedEntityGroup { Value = newChamp });

            respawnsToCleanup.Add(i);
          }
          else {
            OnRespawn?.Invoke();
          }
        }
        else if(!isServer) {
          if(SystemAPI.TryGetSingleton<NetworkId>(out var networkId)) {
            if(networkId.Value == curRespawn.NetworkId) {
              var ticksToRespawn = curRespawn.RespawnTick.TickIndexForValidTick - currTick.TickIndexForValidTick;
              var simTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
              var secondsToStart = (int)math.ceil(ticksToRespawn / simTickRate);
              OnUpdateRespawnCountdown?.Invoke(secondsToStart);
            }
          }
        }
      }

      foreach(var respawnIndex in respawnsToCleanup) {
        respawnBuffer.RemoveAt(respawnIndex);
      }
    }

    ecb.Playback(EntityManager);
    ecb.Dispose();
  }

  protected override void OnStartRunning() {
    if(SystemAPI.HasSingleton<RespawnEntityTag>()) return;
    var respawnPrefab = SystemAPI.GetSingleton<MobaPrefabs>().RespawnEntity;
    EntityManager.Instantiate(respawnPrefab);
  }
}