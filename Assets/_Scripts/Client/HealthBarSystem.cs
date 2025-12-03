using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

[UpdateAfter(typeof(TransformSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct HealthBarSystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    state.RequireForUpdate<UIPrefabs>();
  }

  public void OnUpdate(ref SystemState state) {
    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
    var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

    //Spawns the healthbar object for any entities that don't have one yet
    foreach(var (transform,hpOffset,maxHP,entity) in 
        SystemAPI.Query<LocalTransform,HealthBarOffset,MaxHitPoints>()
        .WithNone<HealthBarUIReference>()
        .WithEntityAccess()) {

      var healthBarPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().HealthBar;
      var spawnPos = transform.Position + hpOffset.Value;
      var newHpBar = Object.Instantiate(healthBarPrefab, spawnPos, Quaternion.identity);
      // We pass these in as max because we know it's effectively spawning the component
      // on an enemy that's full hp. 
      // However if it wasn't on full hp the update system would redraw that next frame.
      SetHealthBar(newHpBar, maxHP.Value, maxHP.Value);
      ecb.AddComponent(entity, new HealthBarUIReference { Value = newHpBar});
    }

    // Moves the position and value of the hp bars
    foreach(var (transform, hpOffset, maxHP, currentHp, hpUI, entity) in
       SystemAPI.Query<LocalTransform, HealthBarOffset, MaxHitPoints, CurrentHitPoints, HealthBarUIReference>()
       .WithEntityAccess()) {

      // We should optimize this later to only update when position changes
      var hpPosition = transform.Position + hpOffset.Value;
      hpUI.Value.transform.position = hpPosition;
      // We should optimize this later to only update when hp changes
      SetHealthBar(hpUI.Value, currentHp.Value, maxHP.Value);
    }

    foreach(var (hpUI, entity) in SystemAPI.Query<HealthBarUIReference>()
        .WithNone<LocalTransform>().WithEntityAccess()) {
      // If an entity no longer has max HP, we remove its health bar.
      Object.Destroy(hpUI.Value);
      ecb.RemoveComponent<HealthBarUIReference>(entity);
    }
  }

  private void SetHealthBar(GameObject hpCanvas, int currHP, int maxHP) {
    var hpSlider = hpCanvas.GetComponentInChildren<Slider>();
    hpSlider.maxValue = maxHP;  
    hpSlider.value = currHP;
    hpSlider.minValue = 0;
  }
}