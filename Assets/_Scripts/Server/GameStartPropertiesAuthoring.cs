using Unity.Entities;
using UnityEngine;

class GameStartPropertiesAuthoring : MonoBehaviour {
  public int MaxPlayersPerTeam;
  public int MinPlayersToStartGame;
  public int CountdownTime;
  public Vector3[] SpawnOffset;

  class GameStartPropertiesAuthoringBaker : Baker<GameStartPropertiesAuthoring> {
    public override void Bake(GameStartPropertiesAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.None);//Data only component

      AddComponent(entity, new GameStartProperties {
        CountdownTime = authoring.CountdownTime,
        MaxPlayersPerTeam = authoring.MaxPlayersPerTeam,
        MinPlayersToStart = authoring.MinPlayersToStartGame
      });

      AddComponent<TeamPlayerCounter>(entity);

      var spawnOffset = AddBuffer<SpawnOffset>(entity);
      if(authoring.SpawnOffset.Length <= 0) {
        Debug.LogError("Spawn offsets were not set.");
      }
      else {
        foreach(var offset in authoring.SpawnOffset) {
          spawnOffset.Add(new SpawnOffset { Value = offset });
        }
      }
    }
  }
}