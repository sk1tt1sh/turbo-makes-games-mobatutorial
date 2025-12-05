using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

class ChampionAuthoring : MonoBehaviour {
  public float MoveSpeed;
  public float DashSpeed;
  public float DashDistance;

  class ChampionAuthoringBaker : Baker<ChampionAuthoring> {
    public override void Bake(ChampionAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);

      AddComponent<ChampTag>(entity);
      AddComponent<NewChampTag>(entity);
      AddComponent<MobaTeam>(entity);
      AddComponent<URPMaterialPropertyBaseColor>(entity);
      AddComponent<ChampMoveTargetPosition>(entity);
      AddComponent(entity, new CharacterMoveSpeed { 
        Value = authoring.MoveSpeed,
        DashSpeed = authoring.DashSpeed,
        DashDistance = authoring.DashDistance
      });
      AddComponent<ChampTargetEntity>(entity);
      AddComponent<AbilityInput>(entity);
      AddComponent<AimInput>(entity);
      AddComponent<NetworkEntityReference>(entity);
    }
  }
}