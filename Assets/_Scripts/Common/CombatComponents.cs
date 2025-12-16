using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public struct MaxHitPoints : IComponentData {
  public int Value;
}

public struct CurrentHitPoints : IComponentData {
  [GhostField] public int Value;
}

//This is a buffer in the event multiple damage events occur during a frame
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct DamageBufferElement : IBufferElementData {
  public int Value;
  public NetworkTick Tick { get; set; }
  public int DealingEntity;
}

//This sets up so that this data is only sync'd to other predicted clients as it is not necessary on the
//owner client. We already know the value is correct on this client.
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct DamageThisTick : ICommandData {
  public int Value;
  public NetworkTick Tick { get; set; }
}

public struct AbilityPrefabs : IComponentData {
  public Entity SkillShotAbility;
  public Entity AoeAbility;
  public Entity ChargeAttackAbility;
}

public struct DestroyOnTimer : IComponentData {
  public float Value;
}

public struct DestroyAtTick : IComponentData {
  [GhostField] public NetworkTick Value;
}

public struct DestroyEntityTag : IComponentData { }

public struct DamageOnTrigger : IComponentData {
  public int Value;
  public bool DestroyOnHit;
}

public struct AlreadyDamagedEntity : IBufferElementData {
  public Entity Value;
}

// Used to match client predicted spawns with server authoritative spawns
public struct PredictedSpawnData : IComponentData {
  [GhostField] public NetworkTick SpawnTick;
  [GhostField] public int OwnerNetworkId;
}

public struct AbilityCooldownTicks : IComponentData {
  public uint AoeAbility;
  public uint SkillShotAbility;
  public uint ChargeAbility;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct AbilityCooldownTargetTicks : ICommandData {
  public NetworkTick Tick { get; set; }
  public NetworkTick AoeAbility;
  public NetworkTick SkillShotAbility;
  public NetworkTick ChargeAbility;
}

public struct AimSkillShotTag : IComponentData { }

public struct AimChargeAbilityTag : IComponentData { }

public struct AbilityMoveSpeed : IComponentData {
  public float Value;
}

public struct AutoAttackMoveSpeed : IComponentData {
  public float Value;
}

public struct ChargeAbilityOwner : IComponentData {
  [GhostField] public Entity Owner;
}

public struct NpcTargetRadius : IComponentData {
  public float Value;
}

public struct NpcTargetEntity : IComponentData {
  [GhostField] public Entity Value;
}

public struct NpcAttackProperties : IComponentData {
  public float3 FirePointOffset;
  public uint CooldownTickCount;
  public Entity AttackPrefab;
}

public struct NpcAttackCoolDown : ICommandData {
  public NetworkTick Tick { get; set; }
  public NetworkTick Value;
}

public struct GameOverOnDestroyTag : IComponentData { }

public struct AutoAttackTarget : IComponentData {
  public int Value;
}
