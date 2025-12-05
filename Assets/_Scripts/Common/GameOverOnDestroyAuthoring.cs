using Unity.Entities;
using UnityEngine;

class GameOverOnDestroyAuthoring : MonoBehaviour {


  class GameOverOnDestroyAuthoringBaker : Baker<GameOverOnDestroyAuthoring> {
    public override void Bake(GameOverOnDestroyAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.None);
      AddComponent<GameOverOnDestroyTag>(entity);
    }
  }
}