using Unity.Entities;
using UnityEngine;

class AbilityMoveSpeedAuthoring : MonoBehaviour {
  public float AbilityMoveSpeed;


  class AbilityMoveSpeedAuthoringBaker : Baker<AbilityMoveSpeedAuthoring> {
    public override void Bake(AbilityMoveSpeedAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new AbilityMoveSpeed { Value = authoring.AbilityMoveSpeed });
    }
  }
}