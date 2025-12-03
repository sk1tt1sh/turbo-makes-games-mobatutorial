using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct AbilityMoveSystem : ISystem {
  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    foreach(var (ability, transform) in 
        SystemAPI.Query<AbilityMoveSpeed, RefRW<LocalTransform>>()
        .WithAll<Simulate>()) {
      transform.ValueRW.Position += transform.ValueRO.Forward() * ability.Value * SystemAPI.Time.DeltaTime;
    }
  }
}
