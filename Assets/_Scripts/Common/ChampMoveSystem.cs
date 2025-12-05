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
        SystemAPI.Query<
          RefRW<LocalTransform>,
          RefRO<ChampMoveTargetPosition>,
          RefRO<CharacterMoveSpeed>
          >()
        .WithNone<ChampDashingTag>()
        .WithAll<Simulate>()
        .WithEntityAccess()) {

      //Gets the value at the current network tick as this could potentially change
      float3 moveTarget;


      //Check if the champ has an autoattack target and override the move position
      
      if(SystemAPI.HasComponent<ChampTargetEntity>(entity)) {
        var target = SystemAPI.GetComponentRW<ChampTargetEntity>(entity);
        var hasLocalTransform = SystemAPI.HasComponent<LocalTransform>(target.ValueRO.Target);

        //Debug.Log($"ChampTargetEntity exists. Target Entity: {target.ValueRO.Target.Index}, " +
        //  $"Has Transform:{hasLocalTransform.ToString()} on {(state.WorldUnmanaged.IsServer()?"server":"client")}");

        if(hasLocalTransform) {
          var targetPos = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.Target);
          moveTarget = targetPos.Position;
          moveTarget.y = transform.ValueRO.Position.y;

          //TODO: Add a component to champ for their auto attack range
          if(math.distance(transform.ValueRO.Position, moveTarget) < 9f) {
            //Debug.Log($"Within auto-range not moving - {math.distance(transform.ValueRO.Position, moveTarget)}");
            continue;
          }
        }
        else {
          moveTarget = movePosition.ValueRO.Value;
          // This prevents the player from moving into the ground
          moveTarget.y = transform.ValueRO.Position.y;
          //if(state.WorldUnmanaged.IsServer())
            //Debug.LogWarning("Target entity has no transform (server)");
          //Proably need to remove that target entity. The champion will probably move to last target position
          //We might be able to use a hack on the component data and set the position as the target moves
        }
      }
      else {
        moveTarget = movePosition.ValueRO.Value;
        // This prevents the player from moving into the ground
        moveTarget.y = transform.ValueRO.Position.y;

        if(math.distance(transform.ValueRO.Position, moveTarget) < 1f) {
          //If the move distance (Likely an absolute value) is too small don't do anything. 
          //Debug.Log("Move target reached");
          continue;
        }
      }

      //Debug.Log($"Moving. The moveTarget value {(moveTarget.Equals(movePosition.ValueRO.Value)?"is equal":"is not equals")} " +
      //  $"to the movePosition input value");
      float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
      var moveVector = moveDirection * moveSpeed.ValueRO.Value * deltaTime;
      //Debug.Log($"Move Vector {moveVector}");
      transform.ValueRW.Position += moveVector;
      //This essentially points the character in the direction that it's going
      transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
      //Debug.Log($"About to move toward: {moveTarget}. Vector: {moveVector}");
    }
  }
}