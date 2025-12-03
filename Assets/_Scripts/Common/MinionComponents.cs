using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public struct MinionTag : IComponentData { }
public struct NewMinionTag : IComponentData { }

public struct MinionPathPosition : IBufferElementData {
  //Quantization is what controls precision of the float3 values
  [GhostField(Quantization = 0)] public float3 Value;
}

public struct MinionPathIndex : IComponentData {
  [GhostField] public byte Value;
}