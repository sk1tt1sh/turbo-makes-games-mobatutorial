using Unity.Entities;
using UnityEngine;

/// <summary>
/// Workflow:
///   Create struct[s] with values to manipulate
///   Create baker script <T>Authoring for adding the values to the entity
///   Add the authoring script to the appropriate prefab[s]
/// </summary>

class HitPointsAuthoring : MonoBehaviour {
  public int MaxHitpoints;
  public Vector3 HealtBarOffset;

  class HitPointsAuthoringBaker : Baker<HitPointsAuthoring> {

    public override void Bake(HitPointsAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new CurrentHitPoints { Value = authoring.MaxHitpoints });
      AddComponent(entity, new MaxHitPoints { Value = authoring.MaxHitpoints });
      AddBuffer<DamageBufferElement>(entity);
      AddBuffer<DamageThisTick>(entity);
      AddComponent(entity, new HealthBarOffset { Value = authoring.HealtBarOffset });
    }
  }
}