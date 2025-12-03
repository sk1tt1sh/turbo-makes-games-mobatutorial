using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

class MinionAuthoring : MonoBehaviour {
  public float MoveSpeed;


  class MinionPrefabAuthoringBaker : Baker<MinionAuthoring> {
    public override void Bake(MinionAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent<MinionTag>(entity);
      AddComponent<NewMinionTag>(entity);
      AddComponent(entity, new CharacterMoveSpeed { Value = authoring.MoveSpeed });
      AddComponent<MinionPathIndex>(entity);
      AddBuffer<MinionPathPosition>(entity);
      AddComponent<MobaTeam>(entity);
      AddComponent<URPMaterialPropertyBaseColor>(entity);
    }
  }
}