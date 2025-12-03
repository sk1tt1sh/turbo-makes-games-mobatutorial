# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity DOTS (Data-Oriented Technology Stack) multiplayer MOBA-style game tutorial using ECS architecture with Netcode for gameplay synchronization.

- **Unity Version**: 6000.2.10f1
- **Rendering**: URP 17.2.0
- **Networking**: Unity Netcode 1.9.3

## Build and Run

Open the project in Unity 6. The game uses standard Unity build process with no custom build scripts.

**Play Mode**: Open `Assets/Scenes/ConnectionScene.unity` as the entry point. Players choose Host/Server/Client role and team before entering the game.

## Architecture

### DOTS/ECS Structure

All gameplay code lives in `Assets/_Scripts/` organized by execution context:

- **Client/** - Input systems, camera, UI controllers, client-only components
- **Server/** - Server authoritative processing (game entry, champion spawning)
- **Common/** - Shared systems, components, and authoring scripts

### Component Organization

Components are pure data structs implementing `IComponentData`:

- **ChampionComponents.cs** - Movement, team, identification (ChampTag, MobaTeam, CharacterMoveSpeed, ChampMoveTargetPosition)
- **CombatComponents.cs** - Health, damage buffers, abilities, timers (MaxHitPoints, CurrentHitPoints, DamageBufferElement, DamageThisTick)
- **RpcComponents.cs** - Network RPCs (MobaTeamRequest)

### System Update Order

Systems use `[UpdateInGroup]` attributes for execution ordering:

1. `GhostInputSystemGroup` - Input capture (ChampMoveInputSystem, AbilityInputSystem)
2. `PredictedSimulationSystemGroup` - Gameplay logic with network prediction
   - Damage systems ordered: DamageOnTriggerSystem → CalculateFrameDamageSystem → ApplyDamageSystem
3. `PhysicsSystemGroup` - Physics triggers for damage detection

### Authoring/Baker Pattern

Every networked prefab has authoring MonoBehaviour + Baker class pairs:

```csharp
class ChampionAuthoring : MonoBehaviour {
    public float MoveSpeed;
}

class ChampionAuthoringBaker : Baker<ChampionAuthoring> {
    public override void Bake(ChampionAuthoring authoring) {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<ChampTag>(entity);
        AddComponent(entity, new CharacterMoveSpeed { Value = authoring.MoveSpeed });
    }
}
```

### Network Architecture

**Client-Server Model**:
- Server is authoritative, clients predict
- Ghost entities sync networked data automatically
- Input components use `IInputComponentData` for network synchronization

**Connection Flow**:
1. ConnectionScene UI → ClientConnectionManager (MonoBehaviour)
2. Creates client/server worlds via ClientServerBootstrap
3. ClientRequestGameEntrySystem sends MobaTeamRequest RPC
4. ServerProcessGameEntryRequestSystem spawns champion

### Key Code Patterns

**Entity Command Buffers**: Used for deferred entity modifications to avoid iterator invalidation

**Network Tick Awareness**: Systems check `firstFullTickPrediction` to prevent duplicate spawning during prediction resimulation

**Query Pattern**:
```csharp
SystemAPI.Query<RefRW<Position>, Velocity>()
    .WithAll<Simulate>()
    .WithNone<DisabledTag>()
    .WithEntityAccess()
```

### Input Actions

Defined in `Assets/Settings/MobaInputActions.inputactions`:
- `SelectMovePosition` - Right mouse button (movement target)
- `AoeAbility` - Q key
- `SkillShotAbility` - W key
- `ConfirmSkillShotAbility` - Left mouse button

## Key Prefabs

Located in `Assets/Prefabs/`:
- **ChampionPrefab** - Playable character with all combat and network components
- **AoeSphere** - AOE ability effect with damage on trigger
- **MinionPrefab**, **Tower**, **Base** - Game entities

Prefab references stored in `MobaPrefabs` singleton component via `MobaPrefabsAuthoring`.

## Development Notes

- Debug.Log calls disable Burst compilation for affected systems
- Destruction: Server destroys entities, clients hide until network sync
- Team colors applied via `URPMaterialPropertyBaseColor` in InitializeCharacterSystem
- Movement locks Y-axis to keep characters on ground plane

# Code style
- Use 2 space indents
- Opening brackets on end of line
- Use concrete types do not use var

# Workflow
- Suggest changes in git diff style
- Do not suggest making edits unless specifically asked
