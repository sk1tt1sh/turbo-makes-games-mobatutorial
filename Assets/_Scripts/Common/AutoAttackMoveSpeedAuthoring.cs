using Unity.Entities;
using UnityEngine;

class AutoAttackMoveSpeedAuthoring : MonoBehaviour {

  public float Speed;

  class AutoAttackMoveSpeedAuthoringAuthoringBaker : Baker<AutoAttackMoveSpeedAuthoring> {
    public override void Bake(AutoAttackMoveSpeedAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new AutoAttackMoveSpeed { Value = authoring.Speed });
    }
  }
}
