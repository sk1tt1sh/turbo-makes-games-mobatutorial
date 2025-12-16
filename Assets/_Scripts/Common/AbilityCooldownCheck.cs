using Unity.Entities;
using Unity.NetCode;

public static class AbilityCooldownCheck {

  public static bool IsOnCooldown(ref NetworkTime netTimeSingleton, NetworkTick currentTick,
      DynamicBuffer<AbilityCooldownTargetTicks> abilityCooldownTargetTick, AbilitiesList ability) {
    var curTargetTicks = new AbilityCooldownTargetTicks();
    for(uint i = 0; i < netTimeSingleton.SimulationStepBatchSize; i++) {
      var testTick = currentTick;
      testTick.Subtract(i);
      if(!abilityCooldownTargetTick.GetDataAtTick(testTick, out curTargetTicks)) {
        //Indicates we cannot get data at the current testing ticket
        switch(ability) {
          case AbilitiesList.Aoe:
            curTargetTicks.AoeAbility = NetworkTick.Invalid;
            break;
          case AbilitiesList.Charge:
            curTargetTicks.ChargeAbility = NetworkTick.Invalid;
            break;
          case AbilitiesList.SkillShot:
            curTargetTicks.SkillShotAbility = NetworkTick.Invalid;
            break;
        }
      }
      switch(ability) {
        case AbilitiesList.Aoe:
          if(curTargetTicks.AoeAbility == NetworkTick.Invalid ||
            !curTargetTicks.AoeAbility.IsNewerThan(currentTick)) {
            //Not on cooldown
            return false;
          }
          break;
        case AbilitiesList.SkillShot:
          if(curTargetTicks.SkillShotAbility == NetworkTick.Invalid ||
            !curTargetTicks.SkillShotAbility.IsNewerThan(currentTick)) {
            //Not on cooldown
            return false;
          }
          break;
        case AbilitiesList.Charge:
          if(curTargetTicks.ChargeAbility == NetworkTick.Invalid ||
            !curTargetTicks.ChargeAbility.IsNewerThan(currentTick)) {
            //Not on cooldown
            return false;
          }
          break;
      }
    }
    return true;
  }
}

public enum AbilitiesList {
  Aoe,
  SkillShot,
  Charge
}