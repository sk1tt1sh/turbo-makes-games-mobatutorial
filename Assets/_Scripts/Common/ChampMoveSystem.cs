using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct ChampMoveSystem : ISystem {

  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<GamePlayingTag>();
  }

  public void OnUpdate(ref SystemState state) {
    float deltaTime = SystemAPI.Time.DeltaTime;

    foreach(var (transform, movePosition, moveSpeed, entity) in 
        SystemAPI.Query<RefRW<LocalTransform>,RefRO<ChampMoveTargetPosition>,CharacterMoveSpeed>()
        .WithNone<ChampDashingTag>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {
      
      //Gets the value at the current network tick as this could potentially change
      float3 moveTarget = movePosition.ValueRO.Value;
      moveTarget.y = transform.ValueRO.Position.y; // This prevents the player from moving into the ground

      //If the move distance (Likely an absolute value) is too small don't do anything. 
      if(math.distance(transform.ValueRO.Position, moveTarget) < 0.2f) continue;
            
      float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
      var moveVector = moveDirection * moveSpeed.Value * deltaTime;
      transform.ValueRW.Position += moveVector;
      //This essentially points the character in the direction that it's going
      transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
    }
  }
}