using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct ClientRequestGameEntrySystem : ISystem
{
  private EntityQuery pendingNetworkIdQuery;

  public void OnCreate(ref SystemState state) {
    EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
    pendingNetworkIdQuery = state.GetEntityQuery(builder);
    state.RequireForUpdate(pendingNetworkIdQuery);
    state.RequireForUpdate<ClientTeamRequest>(); // In ClientComponents
  }

  public void OnUpdate(ref SystemState state) {
    ClientTeamRequest requestedTeam = SystemAPI.GetSingleton<ClientTeamRequest>();
    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

    var pendingIds = pendingNetworkIdQuery.ToEntityArray(Allocator.Temp);

    foreach(var pendingNetId in pendingIds) {
      ecb.AddComponent<NetworkStreamInGame>(pendingNetId);

      var reqTeamEntity = ecb.CreateEntity();
      ecb.AddComponent(reqTeamEntity, new MobaTeamRequest { 
        Value = requestedTeam.Value,
      });
      ecb.AddComponent(reqTeamEntity, new SendRpcCommandRequest { 
        TargetConnection = pendingNetId
      });
    }
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}
