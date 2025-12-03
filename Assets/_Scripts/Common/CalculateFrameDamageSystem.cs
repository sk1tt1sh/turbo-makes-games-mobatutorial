using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;


[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct CalculateFrameDamageSystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<NetworkTime>();
  }

  [BurstCompile]
  public void OnUpdate(ref SystemState state) {
    var currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;    

    foreach(var (damageBuffer, damageThisTickBuffer) in
        SystemAPI.Query<DynamicBuffer<DamageBufferElement>,
        DynamicBuffer<DamageThisTick>>()
        .WithAll<Simulate>()) {

      if(damageBuffer.IsEmpty) {
        damageThisTickBuffer.AddCommandData(new DamageThisTick { Tick = currentTick, Value = 0 });
      }
      else {
        //Debug.Log($"damageBuffer length is: [{damageBuffer.Length}]");
        //Debug.Log($"damageThisTickBuffer length is: [{damageThisTickBuffer.Length}]");
        //foreach(var item in damageBuffer) {
        //  Debug.Log($"damageBuffer: [{item.Value}] [{item.Tick}] dealingEntity: [{item.DealingEntity}]");
        //}
        //StringBuilder sb = new StringBuilder();
        //sb.Append("damageThisTickBuffer contents: ");
        //foreach(var item in damageThisTickBuffer) {
        //  if(item.Value != 0) sb.Append($"[{item.Value}] [{item.Tick}] - ");
        //}
        //Debug.Log(sb.ToString());

        var totDmg = 0;
        if(damageThisTickBuffer.GetDataAtTick(currentTick, out DamageThisTick damageThisTick)) {
          //Multiple client ticks/server tick means we have to try to get all those instances
          //Debug.Log($"Damage for tick [{currentTick}] is [{damageThisTick.Value}] - [{state.World.Name}]"); 
          totDmg = damageThisTick.Value;
        }
        //It might be ideal to check the entity (this would be the receiving entity? 
        //and if they already have the damage for the tick from the dealing entity
        //then continue
        foreach(var damage in damageBuffer) {
          //Debug.Log($"Adding [{damage.Value}] to [{totDmg}] from damageBuffer from tick [{damage.Tick}]. [{state.World.Name}]");
          totDmg += damage.Value;
        }
        //Debug.Log($"CalculateFrameDamageSystem adding [{totDmg}] on tick [{currentTick}]. Damage Buffer Length was: [{damageBuffer.Length}] - [{state.World.Name}]");
        damageThisTickBuffer.AddCommandData(new DamageThisTick { Value = totDmg, Tick = currentTick });
        damageBuffer.Clear();
      }
    }
  }
}