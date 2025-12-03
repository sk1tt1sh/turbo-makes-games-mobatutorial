using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PhysicsSystemGroup))] //Runs under predicted sim system group
[UpdateAfter(typeof(PhysicsSimulationGroup))]
//Because we're going to be querying physics world for raycasts
[UpdateBefore(typeof(ExportPhysicsWorld))]
public partial struct NpcTargetSystem : ISystem {
  private CollisionFilter npcTargetFilter;

  [BurstCompile]
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<PhysicsWorldSingleton>();

    npcTargetFilter = new CollisionFilter {
      BelongsTo = 1 << 6, // Target Cast
      CollidesWith = 1 << 1 | 1 << 2 | 1 << 4 // Champions, Minions, Structures
    };
  }

  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    state.Dependency = new NpcTargetingJob {
      CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
      NpcTargetFilter = npcTargetFilter,
      MobaTeamLookup = SystemAPI.GetComponentLookup<MobaTeam>(true)
    }.ScheduleParallel(state.Dependency);
  }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct NpcTargetingJob : IJobEntity {
  [Unity.Collections.ReadOnly] public CollisionWorld CollisionWorld;
  [Unity.Collections.ReadOnly] public CollisionFilter NpcTargetFilter;
  [Unity.Collections.ReadOnly] public ComponentLookup<MobaTeam> MobaTeamLookup;

  [BurstCompile]
  public void Execute(Entity npcEntity, ref NpcTargetEntity targetEntity, in LocalTransform transform,
    in NpcTargetRadius targetRadius) {

    var hits = new NativeList<DistanceHit>(Allocator.TempJob);
    if(CollisionWorld.OverlapSphere(transform.Position, targetRadius.Value, ref hits, NpcTargetFilter)) {
      var closestDist = float.MaxValue;
      var closestEntity = Entity.Null;

      foreach(var hit in hits) {
        if(!MobaTeamLookup.TryGetComponent(hit.Entity, out var mobaTeam)) continue;
        if(mobaTeam.Value == MobaTeamLookup[npcEntity].Value) continue;
        if(hit.Distance < closestDist) {
          closestDist = hit.Distance;//Here it will swap targets (e.g. tower try to figure out lock on)
          closestEntity = hit.Entity;
        }
      }

      targetEntity.Value = closestEntity;
    }
    else {
      targetEntity.Value = Entity.Null;
    }

    hits.Dispose();
  }
}