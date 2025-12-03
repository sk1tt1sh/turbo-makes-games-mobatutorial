using Unity.NetCode;
using UnityEngine;

public struct MobaTeamRequest : IRpcCommand {

  public TeamType Value;
}

public struct PlayersRemainingToStart : IRpcCommand {
  public int Value;
}

public struct GameStartTickRpc : IRpcCommand {
  public NetworkTick Value;
}