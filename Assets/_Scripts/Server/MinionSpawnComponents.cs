using Unity.Entities;
using UnityEngine;

public struct MinionSpawnProperties : IComponentData {
  public float TimeBetweenWaves;
  public float TimeBetweenMinions;
  public int NumMinionsWave;
}

public struct MinionSpawnTimers : IComponentData { 
  public float TimeUntilNextWave;
  public float TimeUntilNextMinion;
  public int CountSpawnedInWave;
}

public struct MinionPathContainers : IComponentData {
  public Entity TopLane;
  public Entity MidLane;
  public Entity BotLane;
}