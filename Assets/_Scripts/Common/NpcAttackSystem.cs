using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct NpcAttackSystem : ISystem {
  [BurstCompile]
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<GamePlayingTag>();
    state.RequireForUpdate<NetworkTime>();
    state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
  }

  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    var netTick = SystemAPI.GetSingleton<NetworkTime>();
    var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

    state.Dependency = new NpcAttackJob {
      CurrentTick = netTick.ServerTick,
      TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
      ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
    }.ScheduleParallel(state.Dependency);
  }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct NpcAttackJob : IJobEntity {
  [ReadOnly] public NetworkTick CurrentTick;
  [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

  public EntityCommandBuffer.ParallelWriter ECB;

  [BurstCompile]
  public void Execute(ref DynamicBuffer<NpcAttackCoolDown> attackCooldown, in NpcAttackProperties attackProps,
      in NpcTargetEntity targetEntity, Entity npcEntity, MobaTeam team,
      [ChunkIndexInQuery] int sortKey) {

    //Determines if the entity exists
    if(!TransformLookup.HasComponent(targetEntity.Value)) return;
    if(!attackCooldown.GetDataAtTick(CurrentTick, out var cooldownExpirTick)) {
      cooldownExpirTick.Value = NetworkTick.Invalid;
    }

    var canAttack = !cooldownExpirTick.Value.IsValid || CurrentTick.IsNewerThan(cooldownExpirTick.Value);
    if(!canAttack) return;

    var spawnPos = TransformLookup[npcEntity].Position + attackProps.FirePointOffset;
    var targetPos = TransformLookup[targetEntity.Value].Position;

    var newAttack = ECB.Instantiate(sortKey, attackProps.AttackPrefab);
    var newAttkTransf = LocalTransform.FromPositionRotation(spawnPos,
      quaternion.LookRotationSafe(targetPos - spawnPos, math.up()));

    ECB.SetComponent(sortKey, newAttack, newAttkTransf);
    ECB.SetComponent(sortKey, newAttack, team);

    var newCooldownTick = CurrentTick;
    newCooldownTick.Add(attackProps.CooldownTickCount);
    attackCooldown.AddCommandData(new NpcAttackCoolDown { Tick = CurrentTick, Value = newCooldownTick });
  }
}