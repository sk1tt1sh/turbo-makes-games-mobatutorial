using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct ChampTag : IComponentData { }

public struct NewChampTag : IComponentData { }

public struct OwnerChampTag : IComponentData { }

public struct ChampDashingTag : IComponentData { }

public struct MobaTeam : IComponentData {
  [GhostField] public TeamType Value;
}

public struct CharacterMoveSpeed : IComponentData {
  //Make this a ghost component if we want to give variable speed
  public float Value;
  public float DashSpeed;
  public float DashDistance;
}

public struct ChampDashData : IComponentData {
  public float DistanceRemaining;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct ChampMoveTargetPosition : IInputComponentData {
  //Quant 0 means the full float value will send the whole float.
  //Setting to higher will reduce the amount of data sent
  [GhostField(Quantization = 1)] public float3 Value;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct ChampTargetGhost : IInputComponentData {
  [GhostField] public int TargetId;
}

public struct ChampAutoAttackProperties : IComponentData {
  public float Range;
  public uint CooldownTicks;
  public float FirePointOffset;
  public Entity AttackPrefab;
}

//This will use the "Input.Set()" event if it's "FirstFullServerTick"
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct AbilityInput : IInputComponentData {
  [GhostField] public InputEvent AoeAbility;
  [GhostField] public InputEvent SkillShotAbility;
  [GhostField] public InputEvent ConfirmSkillShotAbility;
  [GhostField] public InputEvent ChargeAttack;
  [GhostField] public InputEvent ConfirmChargeAttack;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct AimInput : IInputComponentData {
  [GhostField(Quantization = 0)] public float3 Value;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct AutoAttackCooldown : ICommandData {
  public NetworkTick Tick { get; set; }
  public NetworkTick Value;
}