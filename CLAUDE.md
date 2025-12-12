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

### Scenes

- **ConnectionScene.unity** - Entry point with connection UI and team selection
- **MobaScene.unity** - Main gameplay scene loaded after connection
- **MobaScene/MobaEntities.unity** - Sub-scene containing game entities

## Architecture

### DOTS/ECS Structure

All gameplay code lives in `Assets/_Scripts/` organized by execution context:

- **Client/** - Input systems, camera, UI controllers, client-only components
- **Server/** - Server authoritative processing (game entry, champion spawning, minion spawning)
- **Common/** - Shared systems, components, and authoring scripts
- **Helpers/** - Utility systems (LoadConnectionSceneSystem)

### Component Organization

Components are pure data structs implementing `IComponentData`:

- **ChampionComponents.cs** - Movement, team, input, auto-attack (ChampTag, MobaTeam, CharacterMoveSpeed, ChampMoveTargetPosition, AbilityInput, AimInput, ChampAutoAttackProperties, ChampDashingTag)
- **CombatComponents.cs** - Health, damage, abilities, timers (MaxHitPoints, CurrentHitPoints, DamageBufferElement, DamageThisTick, AbilityPrefabs, AbilityCooldownTicks, DamageOnTrigger, AutoAttackTarget)
- **RpcComponents.cs** - Network RPCs (MobaTeamRequest)
- **MinionComponents.cs** - Minion AI (MinionTag, MinionPathPosition, MinionPathIndex)
- **RespawnComponents.cs** - Player respawn (RespawnEntityTag, RespawnBufferElement, RespawnTickCount, PlayerSpawnInfo)
- **GameplayComponents.cs** - Game state (GamePlayingTag, GameStartTick, GameOverTag, WinningTeam)
- **ClientComponents.cs** - Client-specific (ClientTeamRequest)

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
- `AoeAbility` - Q key (Area of Effect ability)
- `SkillShotAbility` - W key (Skill shot ability, requires aiming)
- `ConfirmSkillShotAbility` - Left mouse button (confirm skill shot direction)
- `ChargeAttack` - E key (Dash/Charge ability)
- `ConfirmChargeAttack` - Left mouse button (confirm charge direction)

## Key Prefabs

Located in `Assets/Prefabs/` organized by subdirectory:

**Players/** (Assets/Prefabs/Players/):
- **ChampionPrefab** - Playable character with all combat and network components
- **Aim_Skillshot** - Visual indicator for aiming skill shot abilities
- **Aim_Charge** - Visual indicator for aiming charge/dash abilities

**Effects/** (Assets/Prefabs/Effects/):
- **AoeSphere** - AOE ability effect with damage on trigger
- **SkillShotAbility** - Projectile for skill shot ability
- **ChargeEffect** - Visual effect for dash/charge ability
- **AutoAttack-Ranged** - Projectile for champion ranged auto-attacks
- **TowerShot** - Projectile for tower attacks

**NPC/** (Assets/Prefabs/NPC/):
- **MinionPrefab** - AI-controlled minions that follow predefined paths
- **MinionShot** - Projectile for minion attacks

**Structures/** (Assets/Prefabs/Structures/):
- **Tower** - Defensive structures with auto-attack capabilities
- **Base** - Main base structure (victory condition when destroyed)

**Game Entities**:
- **GameOverEntity** - Singleton entity for game over state
- **RespawnEntity** - Singleton entity managing player respawns

Prefab references stored in `MobaPrefabs` singleton component via `MobaPrefabsAuthoring`.

## Key Gameplay Features

### Champion Abilities
Champions have three abilities controlled by input:
1. **AOE Ability (Q)** - Instant area-of-effect damage around champion
2. **Skill Shot Ability (W)** - Aimed projectile that requires directional confirmation
3. **Charge/Dash Ability (E)** - Directional dash with hit detection during movement

All abilities have cooldown timers synchronized across network.

### Combat System
- **Auto-Attack** - Champions and NPCs automatically attack nearby enemies
- **Damage Pipeline**: DamageOnTriggerSystem → CalculateFrameDamageSystem → ApplyDamageSystem
- **Damage Buffering** - DamageBufferElement accumulates damage within a frame for atomic application
- **Team-Based Targeting** - Entities only attack opposing team members

### Minion System
- **Spawn Waves** - Server spawns minions at regular intervals for both teams
- **Path Following** - Minions follow predefined waypoint paths (MinionPathPosition buffer)
- **AI Behavior** - Minions automatically target and attack nearby enemies while progressing along path

### Game State Management
- **Game Start Countdown** - Countdown timer before gameplay begins (CountdownToGameStartSystem)
- **Victory Condition** - Game ends when a team's base is destroyed (GameOverOnDestroyTag)
- **Respawn System** - Dead champions respawn after a timed delay (RespawnChampSystem, RespawnBufferElement)

### UI Systems
- **Health Bars** - Floating health bars above all entities (HealthBarSystem)
- **Ability Cooldowns** - UI indicators for ability cooldown status (AbilityCooldownUISystem)
- **Game Start UI** - Countdown timer display (GameStartUIController)
- **Game Over UI** - Victory/defeat screen (GameOverUIController)
- **Respawn UI** - Death and respawn timer display (RespawnUIController)

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
