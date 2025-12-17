using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;


[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
//[UpdateAfter(typeof(PhysicsSimulationGroup))]
//[UpdateBefore(typeof(ExportPhysicsWorld))]
public partial struct ChampionDashSystem : ISystem {

  [BurstCompile]
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<NetworkTime>();
  }

  public void OnUpdate(ref SystemState state) {
    var netTime = SystemAPI.GetSingleton<NetworkTime>();
    if(!netTime.IsFirstTimeFullyPredictingTick) return;

    var ecb = new EntityCommandBuffer(Allocator.Temp);
    var currentTick = netTime.ServerTick;
    float deltaTime = SystemAPI.Time.DeltaTime;
    var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

    foreach(var (xForm, dashData, targetLoc, moveSpeed, entity) in
        SystemAPI.Query<
          RefRW<LocalTransform>, 
          RefRW<ChampDashData>,
          RefRO<ChampMoveTargetPosition>,
          RefRO<CharacterMoveSpeed>>()
        .WithAll<Simulate, ChampDashingTag>().WithEntityAccess()) {

      float3 moveTarget = targetLoc.ValueRO.Value;
      moveTarget.y = xForm.ValueRO.Position.y; // This prevents the player from moving into the ground

      //If the move distance (Likely an absolute value) is too small don't do anything. 
      //if(math.distance(xForm.ValueRO.Position, moveTarget) < 0.2f) continue;

      float3 moveDirection = math.normalize(moveTarget - xForm.ValueRO.Position);
      var moveVector = moveDirection * moveSpeed.ValueRO.DashSpeed * deltaTime;
      xForm.ValueRW.Position += moveVector;
      //This essentially points the character in the direction that it's going
      xForm.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());

      dashData.ValueRW.DistanceRemaining -= math.length(moveVector);

      if(dashData.ValueRO.DistanceRemaining <= 0f) {
        //Debug.Log("Dash distance consumed");
        ecb.RemoveComponent<ChampDashingTag>(entity);
        ecb.RemoveComponent<ChampDashData>(entity);
      }
    }

    //Ideally we want to move both simultaneously because if the move effect is lagging
    //behind the champion then hits will be somewhat latent.
    //Additionally if the dash completes the transform will be removed a frame too soon
    foreach(var (xForm, abilityOwner, targetLoc, moveSpeed, dashData, entity) in
        SystemAPI.Query<
          RefRW<LocalTransform>,
          RefRO<ChargeAbilityOwner>,
          RefRO<ChampMoveTargetPosition>,
          RefRO<CharacterMoveSpeed>,
          RefRW<ChampDashData>>()
       .WithAll<Simulate, ChargeAbilityOwner>().WithEntityAccess()) {

      float3 moveTarget = targetLoc.ValueRO.Value;
      moveTarget.y = xForm.ValueRO.Position.y; // This prevents the player from moving into the ground

      //If the move distance (Likely an absolute value) is too small don't do anything. 
      //if(math.distance(xForm.ValueRO.Position, moveTarget) < 0.2f) continue;

      float3 moveDirection = math.normalize(moveTarget - xForm.ValueRO.Position);
      var moveVector = moveDirection * moveSpeed.ValueRO.DashSpeed * deltaTime;
      xForm.ValueRW.Position += moveVector;
      //This essentially points the character in the direction that it's going
      xForm.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());

      dashData.ValueRW.DistanceRemaining -= math.length(moveVector);

      if(dashData.ValueRO.DistanceRemaining <= 0f && state.WorldUnmanaged.IsServer()) {
        //Debug.Log("Dash distance consumed");
        ecb.DestroyEntity(entity);
      }
    }

    ecb.Playback(state.EntityManager);
    ecb.Dispose();
  }
}