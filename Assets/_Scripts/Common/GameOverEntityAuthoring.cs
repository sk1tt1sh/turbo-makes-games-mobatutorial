using Unity.Entities;
using UnityEngine;

class GameOverEntityAuthoring : MonoBehaviour {
  class GameOverEntityAuthoringBaker : Baker<GameOverEntityAuthoring> {
    public override void Bake(GameOverEntityAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.None);
      AddComponent<GameOverTag>(entity);
      AddComponent<WinningTeam>(entity);
    }
  }
}