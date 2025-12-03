using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

public struct GamePlayingTag : IComponentData { }

public struct GameStartTick : IComponentData {
  public NetworkTick Value;
}