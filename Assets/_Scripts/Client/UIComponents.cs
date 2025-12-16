using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

//Lets us do init and cleanup logic using ICleanupComponentData
public class HealthBarUIReference : ICleanupComponentData {
  public GameObject Value;

}

public struct HealthBarOffset : IComponentData {
  public float3 Value;
}

public class SkillShotUIReference : ICleanupComponentData {
  public GameObject Value;
}