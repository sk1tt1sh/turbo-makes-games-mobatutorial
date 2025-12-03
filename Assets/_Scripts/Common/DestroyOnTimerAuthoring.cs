using Unity.Entities;
using UnityEngine;

public class DestroyOnTimerAuthoring : MonoBehaviour {
  public float DestroyOnTimer;

  public class DestroyOnTimerAuthoringBaker : Baker<DestroyOnTimerAuthoring> {
    public override void Bake(DestroyOnTimerAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new DestroyOnTimer { Value = authoring.DestroyOnTimer });
    }
  }
}