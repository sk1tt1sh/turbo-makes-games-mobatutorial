using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

class AbilityAuthoring : MonoBehaviour {
  [Header("Prefabs")]
  public GameObject AoeAbility;
  public GameObject SkillShotAbility;
  public GameObject ChargeAbility;

  [Header("Cooldowns - In Seconds")]
  public float AoeAbilityCooldown;
  public float SkillShotAbilityCooldown;
  public float ChargeAbilityCooldown;

  public NetCodeConfig NetcodeConfig;
  private int SimulationTickRate => NetcodeConfig != null ? NetcodeConfig.ClientServerTickRate.SimulationTickRate : 60;

  class AbilityAuthoringBaker : Baker<AbilityAuthoring> {
    public override void Bake(AbilityAuthoring authoring) {
      //This gets the entity that is associated to the game object
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new AbilityPrefabs {
        AoeAbility = GetEntity(authoring.AoeAbility, TransformUsageFlags.Dynamic),
        SkillShotAbility = GetEntity(authoring.SkillShotAbility, TransformUsageFlags.Dynamic),
        ChargeAttackAbility = GetEntity(authoring.ChargeAbility, TransformUsageFlags.Dynamic)
      });
      AddComponent(entity, new AbilityCooldownTicks {
        AoeAbility = (uint)(authoring.AoeAbilityCooldown * authoring.SimulationTickRate),
        SkillShotAbility = (uint)(authoring.SkillShotAbilityCooldown * authoring.SimulationTickRate),
        ChargeAbility = (uint)(authoring.ChargeAbilityCooldown * authoring.SimulationTickRate)
      });
      AddBuffer<AbilityCooldownTargetTicks>(entity);
    }
  }
}