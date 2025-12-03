using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct GameStartProperties : IComponentData {
  public int MaxPlayersPerTeam;
  public int MinPlayersToStart;
  public int CountdownTime;
}

public struct TeamPlayerCounter : IComponentData {
  public int BlueTeamPlayers;
  public int RedTeamPlayers;

  public int TotalPlayers => BlueTeamPlayers + RedTeamPlayers;
}

//Enables spawning players in a pattern
public struct SpawnOffset : IBufferElementData {
  public float3 Value;
}