using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
partial struct NewISystemScript : ISystem {

  public void OnUpdate(ref SystemState state) {
    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
    foreach(var (physMass, mobaTeam, newCharEntity) in SystemAPI.Query<RefRW<PhysicsMass>, MobaTeam>()
        .WithAny<NewChampTag, NewMinionTag>().WithEntityAccess()) {

      physMass.ValueRW.InverseInertia[0] = 0;
      physMass.ValueRW.InverseInertia[1] = 0;
      physMass.ValueRW.InverseInertia[2] = 0;

      float4 teamColor = mobaTeam.Value switch {
        TeamType.Blue => new float4(0, 0, 1, 1),
        TeamType.Red => new float4(1, 0, 0, 1),
        _ => new float4(1)
      };

      ecb.SetComponent(newCharEntity, new URPMaterialPropertyBaseColor { Value = teamColor });
      ecb.RemoveComponent<NewChampTag>(newCharEntity);
      ecb.RemoveComponent<NewMinionTag>(newCharEntity);
    }
    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}