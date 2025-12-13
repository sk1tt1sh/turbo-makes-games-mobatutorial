using Unity.Entities;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;

class ChampionAuthoring : MonoBehaviour {
  [Header("Movement")]
  public float MoveSpeed;
  public float DashSpeed;
  public float DashDistance;

  [Header("AutoAttack")]
  public float AutoAttackRange;
  public float AutoAttackCooldown;
  public float FirePointOffset;
  public GameObject AttackPrefab;

  public NetCodeConfig netCodeConfig;
  public int SimTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;

  class ChampionAuthoringBaker : Baker<ChampionAuthoring> {
    public override void Bake(ChampionAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);

      AddComponent<ChampTag>(entity);
      AddComponent<NewChampTag>(entity);
      AddComponent<MobaTeam>(entity);
      AddComponent<URPMaterialPropertyBaseColor>(entity);
      AddComponent<ChampMoveTargetPosition>(entity);
      AddComponent(entity, new CharacterMoveSpeed { 
        Value = authoring.MoveSpeed,
        DashSpeed = authoring.DashSpeed,
        DashDistance = authoring.DashDistance
      });
      AddComponent(entity, new ChampAutoAttackProperties { 
        Range = authoring.AutoAttackRange,
        CooldownTicks = (uint)(authoring.AutoAttackCooldown*authoring.SimTickRate),
        FirePointOffset = authoring.FirePointOffset,
        AttackPrefab = GetEntity(authoring.AttackPrefab,TransformUsageFlags.Dynamic)
      });
      AddComponent<ChampTargetGhost>(entity);
      AddComponent<AbilityInput>(entity);
      AddComponent<AimInput>(entity);
      AddComponent<NetworkEntityReference>(entity);
      AddBuffer<AutoAttackCooldown>(entity);
    }
  }
}