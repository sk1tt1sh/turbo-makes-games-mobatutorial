using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Analytics;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerProcessGameEntryRequestSystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<MobaPrefabs>();
    state.RequireForUpdate<NetworkTime>();
    state.RequireForUpdate<GameStartProperties>();
    EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<MobaTeamRequest, ReceiveRpcCommandRequest>();
    state.RequireForUpdate(state.GetEntityQuery(builder));
  }

  public void OnUpdate(ref SystemState state) {
    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
    Entity championPrefab = SystemAPI.GetSingleton<MobaPrefabs>().Champion;

    var gamePropsEntity = SystemAPI.GetSingletonEntity<GameStartProperties>();
    var gameStartProps = SystemAPI.GetComponent<GameStartProperties>(gamePropsEntity);
    var teamPlayerCount = SystemAPI.GetComponent<TeamPlayerCounter>(gamePropsEntity);
    var spawnOffsets = SystemAPI.GetBuffer<SpawnOffset>(gamePropsEntity);

    foreach((MobaTeamRequest teamReq, ReceiveRpcCommandRequest reqSrc, Entity reqEntity) in
        SystemAPI.Query<MobaTeamRequest, ReceiveRpcCommandRequest>().WithEntityAccess()) {

      ecb.DestroyEntity(reqEntity);
      ecb.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);

      TeamType reqTeamType = teamReq.Value;

      if(reqTeamType == TeamType.AutoAssign) {
        if(teamPlayerCount.BlueTeamPlayers > teamPlayerCount.RedTeamPlayers)
          reqTeamType = TeamType.Blue;
        else if (teamPlayerCount.RedTeamPlayers <= teamPlayerCount.BlueTeamPlayers)
          reqTeamType = TeamType.Red;
      }

      int clientId = SystemAPI.GetComponent<NetworkId>(reqSrc.SourceConnection).Value;


      Entity newChamp = ecb.Instantiate(championPrefab);
      ecb.SetName(newChamp, "Champion");
      float3 spawnPos;

      switch(reqTeamType) {
        case TeamType.Blue:
          if(teamPlayerCount.BlueTeamPlayers >= gameStartProps.MaxPlayersPerTeam) {
            Debug.Log($"Blue team is full {clientId} is spectating");
            continue;
          }
          spawnPos = new float3(-50, 1, -50);
          spawnPos += spawnOffsets[teamPlayerCount.BlueTeamPlayers].Value;
          teamPlayerCount.BlueTeamPlayers++;
          break;
        case TeamType.Red:
          if(teamPlayerCount.RedTeamPlayers >= gameStartProps.MaxPlayersPerTeam) {
            Debug.Log($"Red team is full {clientId} is spectating");
            continue;
          }
          spawnPos = new float3(50, 1, 50);
          spawnPos += spawnOffsets[teamPlayerCount.RedTeamPlayers].Value;
          teamPlayerCount.RedTeamPlayers++;
          break;
        default: continue;
      }

      LocalTransform newXform = LocalTransform.FromPosition(spawnPos);
      ecb.SetComponent(newChamp, newXform);
      ecb.SetComponent(newChamp, new GhostOwner { NetworkId = clientId });
      ecb.SetComponent(newChamp, new MobaTeam { Value = reqTeamType });
      //This will destroy the champion when the connection drops etc...
      ecb.AppendToBuffer(reqSrc.SourceConnection, new LinkedEntityGroup { Value = newChamp });


      ecb.SetComponent(newChamp, new NetworkEntityReference { Value = reqSrc.SourceConnection });
      ecb.AddComponent(reqSrc.SourceConnection, new PlayerSpawnInfo { 
        SpawnPosition = spawnPos,
        Team = reqTeamType
      });

      var playersRemainingToStart = gameStartProps.MinPlayersToStart - teamPlayerCount.TotalPlayers;
      var gameStartRpc = ecb.CreateEntity();
      if(playersRemainingToStart <= 0 && !SystemAPI.HasSingleton<GamePlayingTag>()) {
        var simTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
        var ticksTillStart = (uint)(simTickRate * gameStartProps.CountdownTime);
        var gameStartTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

        gameStartTick.Add(ticksTillStart);
        ecb.AddComponent(gameStartRpc, new GameStartTickRpc { Value = gameStartTick });

        var gameStartEntity = ecb.CreateEntity();
        ecb.AddComponent(gameStartEntity, new GameStartTick { Value = gameStartTick });
      }
      else {
        ecb.AddComponent(gameStartRpc, new PlayersRemainingToStart { Value = playersRemainingToStart });
      }

      ecb.AddComponent<SendRpcCommandRequest>(gameStartRpc);
    }

    ecb.Playback(state.EntityManager);
    SystemAPI.SetSingleton(teamPlayerCount);

    ecb.Dispose();
  }
}