using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct AbilityCooldownUISystem : ISystem {
  public void OnCreate(ref SystemState state) {
    state.RequireForUpdate<NetworkTime>();
  }

  public void OnUpdate(ref SystemState state) {
    var currentTicks = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
    var abilityCooldownUIController = AbilityCooldownUIController.Instance;

    if(abilityCooldownUIController == null) return;

    foreach(var (cooldownTargetTicks, abilityCooldownTicks) in 
        SystemAPI.Query<DynamicBuffer<AbilityCooldownTargetTicks>, AbilityCooldownTicks>()) {

      if(!cooldownTargetTicks.GetDataAtTick(currentTicks, out AbilityCooldownTargetTicks curTarTick)) {
        curTarTick.AoeAbility = NetworkTick.Invalid;
        curTarTick.SkillShotAbility = NetworkTick.Invalid;
        curTarTick.ChargeAbility = NetworkTick.Invalid;
      }

      if(curTarTick.AoeAbility == NetworkTick.Invalid || currentTicks.IsNewerThan(curTarTick.AoeAbility)) {
        abilityCooldownUIController.UpdateAoeMask(0f);
      }
      else {
        var aoeRemTicks = curTarTick.AoeAbility.TickIndexForValidTick - currentTicks.TickIndexForValidTick;
        var fillAmt = (float)aoeRemTicks / abilityCooldownTicks.AoeAbility;
        abilityCooldownUIController.UpdateAoeMask(fillAmt);
      }
      
      if(curTarTick.SkillShotAbility == NetworkTick.Invalid || currentTicks.IsNewerThan(curTarTick.SkillShotAbility)) {
        abilityCooldownUIController.UpdateSkillShotMask(0f);
      }
      else {
        var ssRemTick = curTarTick.SkillShotAbility.TickIndexForValidTick - currentTicks.TickIndexForValidTick;
        var fillAmt = (float)ssRemTick / abilityCooldownTicks.SkillShotAbility;
        abilityCooldownUIController.UpdateSkillShotMask(fillAmt);
      }

      if(curTarTick.ChargeAbility == NetworkTick.Invalid || currentTicks.IsNewerThan(curTarTick.ChargeAbility)) {
        abilityCooldownUIController.UpdateChargeMask(0f);
      }
      else {
        var ssRemTick = curTarTick.ChargeAbility.TickIndexForValidTick - currentTicks.TickIndexForValidTick;
        var fillAmount = (float)ssRemTick / abilityCooldownTicks.ChargeAbility;
        abilityCooldownUIController.UpdateChargeMask(fillAmount);
      }
    }
  }
}