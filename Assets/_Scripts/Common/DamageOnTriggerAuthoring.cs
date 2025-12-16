using Unity.Entities;
using UnityEngine;

public class DamageOnTriggerAuthoring : MonoBehaviour {
  public int DamageOnTrigger;
  public bool DestroyOnHit;

  class DamageOnTriggerAuthoringBaker : Baker<DamageOnTriggerAuthoring> {
    public override void Bake(DamageOnTriggerAuthoring authoring) {
      var entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new DamageOnTrigger {
        Value = authoring.DamageOnTrigger,
        DestroyOnHit = authoring.DestroyOnHit
      });
      AddBuffer<AlreadyDamagedEntity>(entity);
    }
  }
}