using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateBefore(typeof(ChampMoveSystem))]
partial struct ChampAutoAttackSystem : ISystem {
  [BurstCompile]
  public void OnCreate(ref SystemState state) {

  }

  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    //EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

  }

  [BurstCompile]
  public void OnDestroy(ref SystemState state) {

  }
}