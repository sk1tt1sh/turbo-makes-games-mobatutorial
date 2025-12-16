using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

partial struct SpawnMinionSystem : ISystem {
  [BurstCompile]
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    state.RequireForUpdate<MobaPrefabs>();
    state.RequireForUpdate<GamePlayingTag>();
  }

  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    float deltaTime = SystemAPI.Time.DeltaTime;

    foreach(var (timer, props) in
        SystemAPI.Query<RefRW<MinionSpawnTimers>, RefRO<MinionSpawnProperties>>()) {

      bool shouldSpawn = timer.ValueRO.TimeUntilNextWave <= 0 && timer.ValueRO.TimeUntilNextMinion <= 0;

      timer.ValueRW.TimeUntilNextWave -= deltaTime;
      timer.ValueRW.TimeUntilNextMinion -= deltaTime;

      if(shouldSpawn) {
        SpawnEachLane(ref state);
        timer.ValueRW.CountSpawnedInWave++;
        if(timer.ValueRO.CountSpawnedInWave >= props.ValueRO.NumMinionsWave) {
          timer.ValueRW.CountSpawnedInWave = 0;
          timer.ValueRW.TimeUntilNextWave = props.ValueRO.TimeBetweenWaves;
          timer.ValueRW.TimeUntilNextMinion = props.ValueRO.TimeBetweenMinions;
        }
        else {
          timer.ValueRW.TimeUntilNextMinion = props.ValueRO.TimeBetweenMinions;
        }
      }
    }
  }

  private void SpawnEachLane(ref SystemState state) {
    var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

    var prefab = SystemAPI.GetSingleton<MobaPrefabs>().Minion;
    var pathContainers = SystemAPI.GetSingleton<MinionPathContainers>();

    var topLane = SystemAPI.GetBuffer<MinionPathPosition>(pathContainers.TopLane);
    SpawnOnLane(ecb, prefab, topLane);

    var midLane = SystemAPI.GetBuffer<MinionPathPosition>(pathContainers.MidLane);
    SpawnOnLane(ecb, prefab, midLane);

    var botLane = SystemAPI.GetBuffer<MinionPathPosition>(pathContainers.BotLane);
    SpawnOnLane(ecb, prefab, botLane);
  }

  private void SpawnOnLane(EntityCommandBuffer ecb, Entity minionPrefab, DynamicBuffer<MinionPathPosition> curLane) {
    var newBlue = ecb.Instantiate(minionPrefab);
    for(int i = 0; i < curLane.Length; i++) {
      ecb.AppendToBuffer(newBlue, curLane[i]);
    }
    var blueSpawnTrans = LocalTransform.FromPosition(curLane[0].Value);
    ecb.SetComponent(newBlue, blueSpawnTrans);
    ecb.SetComponent(newBlue, new MobaTeam { Value = TeamType.Blue });

    var newRed = ecb.Instantiate(minionPrefab);
    for(int i = curLane.Length - 1; i >= 0; i--) {
      ecb.AppendToBuffer(newRed, curLane[i]);
    }

    //^1 is a neat shorthand to take the last element of an array.
    var redSpawnTrans = LocalTransform.FromPosition(curLane[^1].Value);
    ecb.SetComponent(newRed, redSpawnTrans);
    ecb.SetComponent(newRed, new MobaTeam { Value = TeamType.Red });
  }
}

