using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

/* This system will operate similar to the npc target system job.
 * As the sphere of the dash prefab is moved and generates collisions 
 * it should then apply damage.
 * Bonus points if I can make the hit enemies move.
 */
/*
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
[UpdateBefore(typeof(ExportPhysicsWorld))]
public partial struct ChampDashHitDetectionSystem : ISystem {
  private CollisionFilter _enemyFilter;

  [BurstCompile]
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<PhysicsWorldSingleton>();
    state.RequireForUpdate<GamePlayingTag>();
    state.RequireForUpdate<NetworkTime>();

    _enemyFilter = new CollisionFilter {
      BelongsTo = 1 << 1,
      CollidesWith = 1 << 1 | 1 << 2 //Minions and Champions
    };
  }

  public void OnUpdate(ref SystemState state) {
    var currentTick = SystemAPI.GetSingleton<NetworkTime>();
    var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

    state.Dependency = new ChampDashHitDetectionJob {
      xFormLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
      CurrentTick = currentTick.ServerTick,
      CollisionFilter = _enemyFilter,
      CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
      ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
    }.ScheduleParallel(state.Dependency);

  }
}

[BurstCompile]
[WithAll(typeof(Simulate), typeof(ChargeAbilityOwner))]
public partial struct ChampDashHitDetectionJob : IJobEntity {
  [ReadOnly] public NetworkTick CurrentTick;
  [ReadOnly] public ComponentLookup<LocalTransform> xFormLookup;
  [ReadOnly] public CollisionWorld CollisionWorld;
  [ReadOnly] public CollisionFilter CollisionFilter;

  public EntityCommandBuffer.ParallelWriter ECB;

  public void Execute(Entity chargeSphere, in MobaTeam team) {
    //Debug.Log($"ChampDashSystem entity {entity.Index} mobateam {team.Value} ");
    var hits = new NativeList<DistanceHit>(Allocator.TempJob);
  }
}
*/