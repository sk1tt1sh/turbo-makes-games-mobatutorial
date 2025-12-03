using Unity.Entities;
using UnityEngine;

public class MinionSpawnPropertiesAuthoring : MonoBehaviour {
  public float TimeBetweenWaves;
  public float TimeBetweenMinionSpawns;
  public int MinionsPerWave;

  public class MinionSpawnPropertiesAuthoringBaker : Baker<MinionSpawnPropertiesAuthoring> {
    public override void Bake(MinionSpawnPropertiesAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.None);
      AddComponent(entity, new MinionSpawnProperties {
        NumMinionsWave = authoring.MinionsPerWave,
        TimeBetweenMinions = authoring.TimeBetweenMinionSpawns,
        TimeBetweenWaves = authoring.TimeBetweenWaves
      });

      AddComponent(entity, new MinionSpawnTimers { 
        CountSpawnedInWave = 0,
        TimeUntilNextMinion = 0f,
        TimeUntilNextWave = authoring.TimeBetweenWaves
      });
    }
  }
}